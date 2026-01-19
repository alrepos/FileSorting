using Domain;
using Infrastructure;

namespace Application
{
    public static class FileGeneratingOrchestrator
    {
        public static void StartGenerating(double? sizeInGb = null, string? inputPath = null)
        {
            var consoleLogger = new ConsoleLogger();
            var generator = new FileGeneratingService(consoleLogger);

            generator.GenerateFileBySize(sizeInGb, inputPath);
        }
    }
}
