using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using HPCsharp;
using Microsoft.Extensions.Logging;

namespace Domain
{
    public class FileSortingService(ILogger logger, FilePathService filePathService, long maxMemoryBytes = 2L * MathData.BytesInGb)
    {
        private readonly ILogger _logger = logger;
        private readonly FilePathService _filePathService = filePathService;
        private readonly long _maxMemoryBytes = maxMemoryBytes;

        private const int StreamBufferSize = 128 * MathData.BytesInKb;
        private const int ChunksChannelCapacity = 8;

        public async Task SortFileAsync(string inputPath, string outputPath)
        {
            var (chunkFiles, rowsCount) = await CreateChunksAsync(inputPath);
            await MergeChunksAsync(chunkFiles, rowsCount, outputPath);
            DeleteChunks(chunkFiles);
        }

        private async Task<(string[] chunkFiles, long rowsCount)> CreateChunksAsync(string inputPath)
        {
            _logger.LogInformation($"Started creating chunks...");

            var chunkFiles = new ConcurrentBag<string>();

            var chunksChannel = Channel.CreateBounded<RowEntity[]>(new BoundedChannelOptions(ChunksChannelCapacity)
            {
                SingleWriter = true,
                SingleReader = false
            });

            Task<long> consumerTask = SortAndWriteChunksAsync(inputPath, chunksChannel.Reader, chunkFiles); // consumer of chunks in memory

            await SplitFileToChunksAsync(inputPath, chunksChannel.Writer); // producer of chunks in memory

            long rowsCount = await consumerTask;

            _logger.LogInformation($"Completed creating of {chunkFiles.Count} chunks with {rowsCount} rows");
            
            return ([.. chunkFiles], rowsCount);
        }

        private async Task SplitFileToChunksAsync(string inputPath, ChannelWriter<RowEntity[]> chunksWriter)
        {
            const short chunksInProgress = 2;
            const short maxChunksInMemory = ChunksChannelCapacity + chunksInProgress; // chunks amount that can exist in memory at once
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

            var readerOptions = new FileStreamOptions()
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

        private async Task<long> SortAndWriteChunksAsync(string inputPath, ChannelReader<RowEntity[]> chunksReader, 
            ConcurrentBag<string> chunkFiles)
        {
            const string chunksFolderName = "sorted_chunks";
            string chunksFolderPath = _filePathService.GetOrCreateNewFolderPath(chunksFolderName, inputPath);
            
            long rowsCount = 0;

            await foreach (RowEntity[] chunkArray in chunksReader.ReadAllAsync())
            {
                string chunkFile = _filePathService.GetNewFilePath(chunksFolderPath);
                chunkFiles.Add(chunkFile);

                chunkArray.SortMergeInPlaceAdaptivePar(); // faster than Array.Sort or .AsParallel().OrderBy

                await WriteChunkToFileAsync(chunkArray, chunkFile);

                Interlocked.Add(ref rowsCount, chunkArray.Length);

                _logger.LogDebug($"Created chunk #{chunkFiles.Count} with {chunkArray.Length} items...");
            }

            return rowsCount;
        }

        private bool IsUsedMemoryHigh()
        {
            return GetUsedHeapMemory() >= _maxMemoryBytes;
        }

        private static long GetUsedHeapMemory()
        {
            return GC.GetTotalMemory(false);
        }

        private async Task MergeChunksAsync(string[] tempFiles, long totalRowsCount, string outputPath)
        {
            _logger.LogInformation($"Started merging of {tempFiles.Length} chunks...");

            if (tempFiles.Length == 0)
            {
                _logger.LogError($"There is no files to merge!");
                return;
            }
            else if (tempFiles.Length == 1)
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                File.Move(tempFiles[0], outputPath);
            }
            else
            {
                const int logProgressStep = 2_000_000;

                var rowComparer = new RowComparer();
                var orderedQueue = new PriorityQueue<(RowEntity Row, int FileIndex), RowEntity>(rowComparer);
                var readers = new StreamReader[tempFiles.Length];
                long linesCount = 0;
            
                try
                {
                    for (int i = 0; i < tempFiles.Length; i++)
                    {
                        var readerOptions = new FileStreamOptions() {
                            Mode = FileMode.Open,
                            Access = FileAccess.Read,
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

                    using var writer = new StreamWriter(outputPath, append: false, Encoding.UTF8, StreamBufferSize);
                    while (orderedQueue.Count > 0)
                    {
                        var (comparedRow, fileIndex) = orderedQueue.Dequeue();
                        writer.WriteLine(comparedRow.ToString());
                        linesCount++;

                        if (linesCount % logProgressStep == 0)
                        {
                            double progress = linesCount / (double)totalRowsCount * 100;
                            _logger.LogDebug($"Merged {progress:F2} % of lines...");
                        }

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
            }

            _logger.LogInformation($"Completed merging of chunks. Result saved to {outputPath}");
        }

        private static async Task WriteChunkToFileAsync(IEnumerable<RowEntity> chunk, string filePath)
        {
            using var writer = new StreamWriter(filePath, append: false, Encoding.UTF8, StreamBufferSize);
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
