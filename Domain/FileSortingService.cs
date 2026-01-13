using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Domain
{
    public class FileSortingService(ILogger logger, long maxMemoryBytes = 1024 * MathData.BytesInMb)
    {
        private readonly ILogger _logger = logger;
        private readonly long _maxMemoryBytes = maxMemoryBytes;

        private const int StreamBufferSize = 128 * MathData.BytesInKb;
        private const int MaxChunksInMemory = 3; // chunk amount that can exist in memory at once
        private const int MinChunkCapacity = 1000;

        public async Task SortFileAsync(string inputPath, string outputPath)
        {
            var tempFiles = await SplitAndSortChunksAsync(inputPath);
            await MergeChunksAsync(tempFiles, outputPath);

            foreach (var file in tempFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        private async Task<List<string>> SplitAndSortChunksAsync(string inputPath)
        {
            var tempFiles = new ConcurrentBag<string>();

            var chunksChannel = Channel.CreateBounded<List<RowEntity>>(new BoundedChannelOptions(capacity: 1)
            {
                SingleWriter = true,
                SingleReader = true
            });

            Task consumerTask = SortAndWriteChunks(tempFiles, chunksChannel.Reader); // consumer of chunks in memory

            await SplitFileToChunks(inputPath, chunksChannel.Writer); // producer of chunks in memory

            await consumerTask;

            return [.. tempFiles];
        }

        private async Task SplitFileToChunks(string inputPath, ChannelWriter<List<RowEntity>> chunksWriter)
        {
            var currentChunk = new List<RowEntity>(MinChunkCapacity);
            long currentChunkBytes = 0;
            string? line;
            long lineCounter = 0;
            int maxChunkBytes = (int)(_maxMemoryBytes / MaxChunksInMemory);
            const int minLineMemory = 50;

            FileStreamOptions readerOptions = new()
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                BufferSize = StreamBufferSize
            };

            using (var reader = new StreamReader(inputPath, readerOptions))
            {

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    RowEntity convertedRow = ConvertLineToRow(line);
                    currentChunk.Add(convertedRow);

                    int currentLineBytes = (line.Length * 2) + minLineMemory;
                    currentChunkBytes += currentLineBytes;

                    lineCounter++;

                    if (lineCounter % MinChunkCapacity == 0)
                    {
                        if (currentChunkBytes >= maxChunkBytes || IsUsedMemoryHigh())
                        {
                            //_logger.LogDebug($"Used memory: {GC.GetTotalMemory(false) / MathData.BytesInMb} MB");

                            await chunksWriter.WriteAsync(currentChunk);

                            currentChunk = new List<RowEntity>(MinChunkCapacity);
                            currentChunkBytes = 0;
                        }
                    }
                }
            }

            // push remaining data
            if (currentChunk.Count > 0)
            {
                await chunksWriter.WriteAsync(currentChunk);
            }

            chunksWriter.Complete();
        }

        private async Task SortAndWriteChunks(ConcurrentBag<string> tempFiles, ChannelReader<List<RowEntity>> chunksReader)
        {
            _logger.LogInformation($"Started sorting and generating chunks...");
            var rowComparer = new RowEntityComparer();
            int generatedChunks = 0;

            await foreach (var chunk in chunksReader.ReadAllAsync())
            {
                //_logger.LogDebug($"Used memory: {GC.GetTotalMemory(false) / MathData.BytesInMb} MB");

                var tempFile = Path.GetTempFileName();
                tempFiles.Add(tempFile);

                RowEntity[] chunkArray = [.. chunk];
                Array.Sort(chunkArray);

                await WriteChunkToFileAsync(chunkArray, tempFile);

                generatedChunks++;
                _logger.LogDebug($"Generated chunk #{generatedChunks} with {chunkArray.Length} items...");

                GC.Collect(); // forced GC to decrease memory usage faster
            }
        }

        private bool IsUsedMemoryHigh()
        {
            return GC.GetTotalMemory(false) >= _maxMemoryBytes;
        }

        private async Task MergeChunksAsync(List<string> tempFiles, string outputPath)
        {
            _logger.LogInformation($"Started merging {tempFiles.Count} chunks...");

            var orderedQueue = new PriorityQueue<(RowEntity Row, int FileIndex), RowEntity>(new RowEntityComparer());
            var readers = new StreamReader[tempFiles.Count];

            try
            {
                for (int i = 0; i < tempFiles.Count; i++)
                {
                    var readerOptions = new FileStreamOptions() {
                        Mode = FileMode.Open,
                        BufferSize = StreamBufferSize
                    };

                    readers[i] = new StreamReader(tempFiles[i], readerOptions);

                    string? line = await readers[i].ReadLineAsync();
                    if (line != null)
                    {
                        RowEntity row = ConvertLineToRow(line);
                        orderedQueue.Enqueue((row, i), row);
                    }
                }

                using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8, StreamBufferSize);
                while (orderedQueue.Count > 0)
                {
                    var (comparedRow, fileIndex) = orderedQueue.Dequeue();
                    writer.WriteLine(comparedRow.ToString());

                    string? nextLine = await readers[fileIndex].ReadLineAsync();
                    if (nextLine != null)
                    {
                        RowEntity nextRow = ConvertLineToRow(nextLine);
                        orderedQueue.Enqueue((nextRow, fileIndex), nextRow);
                    }
                }
            }
            finally
            {
                foreach (var r in readers) 
                { 
                    r?.Dispose();
                }
            }

            _logger.LogInformation($"Completed merging chunks. Result saved to {outputPath}");
        }

        private static async Task WriteChunkToFileAsync(IEnumerable<RowEntity> chunk, string filePath)
        {
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8, StreamBufferSize);
            foreach (RowEntity row in chunk)
            {
                await writer.WriteLineAsync(row.ToString());
            }
        }

        private static RowEntity ConvertLineToRow(string line)
        {
            const string separatorValue = FileGeneratingService.FileRowSeparator;
            int separatorIndex = line.IndexOf(separatorValue);

            if (separatorIndex == -1)
            {
                return new RowEntity(0, line); // to handle rows without correct separator
            }

            ReadOnlySpan<char> numSpan = line.AsSpan(0, separatorIndex);
            long number = long.Parse(numSpan);

            int textIndex = separatorIndex + separatorValue.Length;
            bool isWithText = textIndex < line.Length;
            string text = isWithText ? line.Substring(textIndex) : string.Empty;

            return new RowEntity(number, text);
        }
    }
}
