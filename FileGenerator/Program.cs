using Application;

namespace FileGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var generatingOrchestrator = new FileGeneratingOrchestrator();
            generatingOrchestrator.StartGenerating();
        }
    }
}
