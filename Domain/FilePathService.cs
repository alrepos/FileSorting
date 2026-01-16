using Microsoft.Extensions.Logging;

namespace Domain
{
    public static class FilePathService
    {
        public static string GetDefaultInputFilePath()
        {
            return GetDefaultFilePath("generated_file.txt");
        }

        public static string GetDefaultOutputFilePath()
        {
            return GetDefaultFilePath("sorted_file.txt");
        }

        private static string GetDefaultFilePath(string fileName)
        {
            string defaultFolder = GetOrCreateDefaultFolderPath();

            string defaultFilePath = Path.Combine(defaultFolder, fileName);
            return defaultFilePath;
        }

        private static string GetOrCreateDefaultFolderPath()
        {
            string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            string defaultFolder = Path.Combine(appFolder, "FileSorting");

            Directory.CreateDirectory(defaultFolder); // creates default folder if it does not exist

            return defaultFolder;
        }
    }
}
