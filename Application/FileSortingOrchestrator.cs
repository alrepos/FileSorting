using Domain;
using Infrastructure;

namespace Application
{
    public class FileSortingOrchestrator
    {
        public async Task StartSortingAsync(double? sizeInGb = null, string? inputPath = null, string? outputPath = null)
        {
            var consoleLogger = new ConsoleLogger();
            var filePathService = new FilePathService();

            inputPath = string.IsNullOrWhiteSpace(inputPath) ?
                filePathService.GetDefaultInputFilePath() : inputPath;

            var fileInfo = new FileInfo(inputPath);

            bool isNeededToGenerate = !fileInfo.Exists || 
                (sizeInGb != null && fileInfo.Length < sizeInGb * MathData.BytesInGb);

            if (isNeededToGenerate)
            {
                var generator = new FileGeneratingService(consoleLogger, filePathService);
                generator.GenerateFileBySize(sizeInGb, inputPath);
            }

            outputPath = string.IsNullOrWhiteSpace(outputPath) ?
                filePathService.GetDefaultOutputFilePath() : outputPath;

            var sorter = new FileSortingService(consoleLogger, filePathService);
            await sorter.SortFileAsync(inputPath, outputPath);
        }
    }
}
