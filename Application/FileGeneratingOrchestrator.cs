using Domain;
using Infrastructure;

namespace Application
{
    public static class FileGeneratingOrchestrator
    {
        public static void StartGenerating()
        {
            var consoleLogger = new ConsoleLogger();
            var fileGeneratingService = new FileGeneratingService(consoleLogger);

            fileGeneratingService.GenerateFileBySize(sizeInGb: 0.3);
        }
    }
}
