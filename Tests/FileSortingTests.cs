using Application;
using Domain;

namespace Tests
{
    public class FileSortingTests
    {
        [Fact]
        public void RowEntity_ComparisonTest()
        {
            // Arrange
            var firstApple = new RowEntity(1, "Apple");
            var secondApple = new RowEntity(415, "Apple");
            var firstBanana = new RowEntity(2, "Banana is yellow");

            // Act
            int appleToApple = firstApple.CompareTo(secondApple);
            int appleToBanana = firstApple.CompareTo(firstBanana);
            int bananaToApple = firstBanana.CompareTo(secondApple);

            // Assert
            Assert.True(appleToApple < 0); // 1 < 415
            Assert.True(appleToBanana < 0); // Apple < Banana
            Assert.True(bananaToApple > 0); // Banana > Apple
        }

        [Fact]
        public async Task FileSorting_IntegrationTest()
        {
            // Arrange

            string input = Path.GetTempFileName();
            string output = Path.GetTempFileName();

            var lines = new string[]
            {
                "415. Apple",
                "30432. Something something something",
                "1. Apple",
                "32. Cherry is the best",
                "2. Banana is yellow"
            };

            // Act

            await File.WriteAllLinesAsync(input, lines);

            await FileSortingOrchestrator.StartSorting(inputPath: input, outputPath: output);

            string[] result = await File.ReadAllLinesAsync(output);

            // Assert

            Assert.Equal("1. Apple", result[0]);
            Assert.Equal("415. Apple", result[1]);
            Assert.Equal("2. Banana is yellow", result[2]);
            Assert.Equal("32. Cherry is the best", result[3]);
            Assert.Equal("30432. Something something something", result[4]);

            // Delete temporary files

            if (File.Exists(input))
            {
                File.Delete(input);
            }

            if (File.Exists(output)) 
            {
                File.Delete(output);
            }
        }
    }
}
