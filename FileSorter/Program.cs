using Application;

namespace FileSorter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var sortingOrchestrator = new FileSortingOrchestrator();
            await sortingOrchestrator.StartSortingAsync();
        }
    }
}