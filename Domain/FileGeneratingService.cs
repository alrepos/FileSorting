using System.Text;
using Microsoft.Extensions.Logging;

namespace Domain
{
    public class FileGeneratingService(ILogger logger)
    {
        private readonly ILogger _logger = logger;

        public const string FileRowSeparator = ". ";

        private const double DefaultFileSizeInGb = 0.3;

        private const short MinStringLength = 10;
        private const short MaxStringLength = 100;

        public static short GetAverageStringLength()
        {
            return MinStringLength + ((MaxStringLength - MinStringLength) / 2);
        }

        public void GenerateFileBySize(double? sizeInGb = null, string? inputPath = null)
        {
            sizeInGb ??= DefaultFileSizeInGb;

            inputPath = string.IsNullOrWhiteSpace(inputPath) ? 
                FilePathService.GetDefaultInputFilePath() : inputPath;

            _logger.LogInformation($"Started generating of {sizeInGb} GB file at {inputPath} ...");

            var random = new Random();

            string[] stringPool = GetSourceStringPool(random);

            long targetBytes = (long)(sizeInGb * MathData.BytesInGb);
            long currentBytes = 0;
            int loggedGb = 0;

            const int bufferSize = 128 * MathData.BytesInKb;
            using var writer = new StreamWriter(inputPath, append: false, Encoding.UTF8, bufferSize);

            while (currentBytes < targetBytes)
            {
                string textPart = stringPool[random.Next(stringPool.Length)];
                int randomNumber = random.Next(1, 10_000_000);
                string fileRow = $"{randomNumber}{FileRowSeparator}{textPart}";

                writer.WriteLine(fileRow);

                const byte rowCharSizeInBytes = 1;
                const byte newLineSizeInBytes = 2;

                long fileRowSizeInBytes = (fileRow.Length * rowCharSizeInBytes) + newLineSizeInBytes;
                currentBytes += fileRowSizeInBytes;

                const int logStepInGb = 1;
                double currentGigabytes = currentBytes / (MathData.BytesInGb * (double)logStepInGb);
                bool isNeededToLog = currentGigabytes > loggedGb && currentGigabytes < sizeInGb;

                if (isNeededToLog)
                {
                    _logger.LogDebug($"Generated {loggedGb} GB / {sizeInGb} GB ...");
                    loggedGb += logStepInGb;
                }
            }

            _logger.LogInformation($"Generating completed. File size: {currentBytes / (double)MathData.BytesInGb:F2} GB");
        }

        /// <summary>
        /// Prepares pool of strings to fill the file and have some number of the same strings there
        /// </summary>
        private static string[] GetSourceStringPool(Random random)
        {
            const int poolSize = 10_000;
            var stringPool = new string[poolSize];
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            for (int i = 0; i < poolSize; i++)
            {
                int stringLength = random.Next(MinStringLength, MaxStringLength);
                var stringChars = new char[stringLength];

                for (int j = 0; j < stringLength; j++)
                {
                    stringChars[j] = chars[random.Next(chars.Length)];
                }

                stringPool[i] = new string(stringChars);
            }

            return stringPool;
        }
    }
}
