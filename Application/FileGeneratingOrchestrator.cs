using Domain;
using Infrastructure;

namespace Application
{
    public class FileGeneratingOrchestrator
    {
        public void StartGenerating(double? sizeInGb = null, string? inputPath = null)
        {
            var consoleLogger = new ConsoleLogger();
            var filePathService = new FilePathService();

            var generator = new FileGeneratingService(consoleLogger, filePathService);
            generator.GenerateFileBySize(sizeInGb, inputPath);
        }
    }
}
