using System.Globalization;
using Domain;
using Infrastructure;

namespace Application
{
    public static class FileSortingOrchestrator
    {
        public static async Task StartSorting(double? sizeInGb = null, string? inputPath = null, string? outputPath = null)
        {
            var consoleLogger = new ConsoleLogger();

            inputPath = string.IsNullOrWhiteSpace(inputPath) ?
                FilePathService.GetDefaultInputFilePath() : inputPath;

            if (!File.Exists(inputPath))
            {
                var fileGeneratingService = new FileGeneratingService(consoleLogger);

                fileGeneratingService.GenerateFileBySize(sizeInGb, inputPath);
            }

            outputPath = string.IsNullOrWhiteSpace(outputPath) ?
                FilePathService.GetDefaultOutputFilePath() : outputPath;

            var sorter = new FileSortingService(consoleLogger);
            await sorter.SortFileAsync(inputPath, outputPath);
        }
    }
}
