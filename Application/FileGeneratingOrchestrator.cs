using Domain;
using Infrastructure;

namespace Application
{
    public static class FileGeneratingOrchestrator
    {
        public static void StartGenerating(double? sizeInGb = null, string? inputPath = null)
        {
            var consoleLogger = new ConsoleLogger();
            var fileGeneratingService = new FileGeneratingService(consoleLogger);

            fileGeneratingService.GenerateFileBySize(sizeInGb, inputPath);
        }
    }
}
