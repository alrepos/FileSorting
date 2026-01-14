using System.Collections.Concurrent;
using System.Threading.Channels;
using HPCsharp;
using Microsoft.Extensions.Logging;

namespace Domain
{
    public class FileSortingService(ILogger logger, long maxMemoryBytes = MathData.BytesInGb)
    {
        private readonly ILogger _logger = logger;
        private readonly long _maxMemoryBytes = maxMemoryBytes;

        private const int StreamBufferSize = 128 * MathData.BytesInKb;

        public async Task SortFileAsync(string inputPath, string outputPath)
        {
            var tempFiles = await CreateChunksAsync(inputPath);
            await MergeChunksAsync(tempFiles, outputPath);
            DeleteChunks(tempFiles);
        }

        private async Task<string[]> CreateChunksAsync(string inputPath)
        {
            _logger.LogInformation($"Started creating chunks...");

            var tempFiles = new ConcurrentBag<string>();

            var chunksChannel = Channel.CreateBounded<RowEntity[]>(new BoundedChannelOptions(capacity: 1)
            {
                SingleWriter = true,
                SingleReader = true
            });

            Task consumerTask = SortAndWriteChunks(tempFiles, chunksChannel.Reader); // consumer of chunks in memory

            await SplitFileToChunks(inputPath, chunksChannel.Writer); // producer of chunks in memory

            await consumerTask;

            _logger.LogInformation($"Completed creating of {tempFiles.Count} chunks");
            
            return [.. tempFiles];
        }

        private async Task SplitFileToChunks(string inputPath, ChannelWriter<RowEntity[]> chunksWriter)
        {
            const short maxChunksInMemory = 3; // chunk amount that can exist in memory at once
            const short memoryCheckStep = 10_000;
            const float reservedCapacityMultiplier = 1.1f;

            short averageRowTextLength = FileGeneratingService.GetAverageStringLength();
            int averageRowBytes = RowEntity.GetRowBytes(averageRowTextLength);
            long maxChunkBytes = _maxMemoryBytes / maxChunksInMemory;
            int averageChunkCapacity = (int)(maxChunkBytes / averageRowBytes * reservedCapacityMultiplier);
            var currentChunk = new List<RowEntity>(averageChunkCapacity);

            long currentChunkBytes = 0;
            string? line;
            long lineCounter = 0;

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
                    RowEntity convertedRow = RowEntity.GetRowFromLine(line);
                    currentChunk.Add(convertedRow);

                    int currentRowBytes = convertedRow.GetRowBytes();
                    currentChunkBytes += currentRowBytes;

                    lineCounter++;

                    if (lineCounter % memoryCheckStep == 0)
                    {
                        if (currentChunkBytes >= maxChunkBytes || IsUsedMemoryHigh())
                        {
                            //_logger.LogDebug($"Used memory: {GetUsedHeapMemory() / MathData.BytesInMb} MB");

                            await chunksWriter.WriteAsync([.. currentChunk]);

                            currentChunk.Clear();
                            currentChunkBytes = 0;
                        }
                    }
                }
            }

            // push remaining data
            if (currentChunk.Count > 0)
            {
                await chunksWriter.WriteAsync([.. currentChunk]);
            }

            chunksWriter.Complete();
        }

        private async Task SortAndWriteChunks(ConcurrentBag<string> tempFiles, ChannelReader<RowEntity[]> chunksReader)
        {
            await foreach (RowEntity[] chunkArray in chunksReader.ReadAllAsync())
            {
                //_logger.LogDebug($"Used memory: {GetUsedHeapMemory() / MathData.BytesInMb} MB");

                var tempFile = Path.GetTempFileName();
                tempFiles.Add(tempFile);

                chunkArray.SortMergePar(); // few times faster than Array.Sort or .AsParallel().OrderBy

                await WriteChunkToFileAsync(chunkArray, tempFile);

                _logger.LogDebug($"Created chunk #{tempFiles.Count} with {chunkArray.Length} items...");

                GC.Collect(); // forced GC to decrease memory usage faster
            }
        }

        private bool IsUsedMemoryHigh()
        {
            return GetUsedHeapMemory() >= _maxMemoryBytes;
        }

        private static long GetUsedHeapMemory()
        {
            return GC.GetTotalMemory(false);
        }

        private async Task MergeChunksAsync(string[] tempFiles, string outputPath)
        {
            _logger.LogInformation($"Started merging of {tempFiles.Length} chunks...");

            var orderedQueue = new PriorityQueue<(RowEntity Row, int FileIndex), RowEntity>(new RowEntityComparer());
            var readers = new StreamReader[tempFiles.Length];

            try
            {
                for (int i = 0; i < tempFiles.Length; i++)
                {
                    var readerOptions = new FileStreamOptions() {
                        Mode = FileMode.Open,
                        BufferSize = StreamBufferSize
                    };

                    readers[i] = new StreamReader(tempFiles[i], readerOptions);

                    string? line = await readers[i].ReadLineAsync();
                    if (line != null)
                    {
                        RowEntity row = RowEntity.GetRowFromLine(line);
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
                        RowEntity nextRow = RowEntity.GetRowFromLine(nextLine);
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

            _logger.LogInformation($"Completed merging of chunks. Result saved to {outputPath}");
        }

        private static async Task WriteChunkToFileAsync(IEnumerable<RowEntity> chunk, string filePath)
        {
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8, StreamBufferSize);
            foreach (RowEntity row in chunk)
            {
                await writer.WriteLineAsync(row.ToString());
            }
        }

        private static void DeleteChunks(string[] tempFiles)
        {
            foreach (var file in tempFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }
    }
}
