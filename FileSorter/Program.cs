using Application;
using Domain;
using Infrastructure;

namespace FileSorter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await FileSortingOrchestrator.StartSorting();
        }
    }
}