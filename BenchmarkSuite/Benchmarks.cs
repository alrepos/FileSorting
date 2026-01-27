using System.Threading.Tasks;
using Application;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;

namespace BenchmarkSuite
{
    [SimpleJob(RunStrategy.ColdStart, iterationCount: 3)]
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [ExceptionDiagnoser]
    public class Benchmarks
    {
        [Benchmark]
        public void GenerateDefaultFile()
        {
            var generatingOrchestrator = new FileGeneratingOrchestrator();
            generatingOrchestrator.StartGenerating();
        }

        [Benchmark]
        public async Task SortDefaultFile()
        {
            var sortingOrchestrator = new FileSortingOrchestrator();
            await sortingOrchestrator.StartSortingAsync();
        }
    }
}
