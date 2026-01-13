using System.Globalization;
using Domain;
using Infrastructure;

namespace Application
{
    public static class FileSortingOrchestrator
    {
        public static async Task StartSorting()
        {
            var consoleLogger = new ConsoleLogger();

            string generatedFile = FilePathService.GetDefaultGeneratedFilePath();

            if (!File.Exists(generatedFile))
            {
                var fileGeneratingService = new FileGeneratingService(consoleLogger);

                fileGeneratingService.GenerateFileBySize(sizeInGb: 0.3);
            }

            string sortedFile = FilePathService.GetDefaultSortedFilePath();

            var sorter = new FileSortingService(consoleLogger);
            await sorter.SortFileAsync(generatedFile, sortedFile);
        }
    }
}
