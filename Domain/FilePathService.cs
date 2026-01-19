namespace Domain
{
    public static class FilePathService
    {
        public static string GetNewFilePath(string? fileFolder = null)
        {
            fileFolder = string.IsNullOrWhiteSpace(fileFolder) ?
                GetOrCreateDefaultFolderPath() : fileFolder;

            string fileName = $"{Guid.NewGuid()}.txt";
            string filePath = Path.Combine(fileFolder, fileName);

            if (File.Exists(filePath))
            {
                return GetNewFilePath(fileFolder);
            }

            return filePath;
        }

        public static string GetOrCreateDefaultFolderPath()
        {
            string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            string defaultFolder = Path.Combine(appFolder, "FileSorting");

            Directory.CreateDirectory(defaultFolder); // creates folder if it does not exist

            return defaultFolder;
        }

        public static string GetOrCreateNewFolderPath(string newFolderName, string? inputPath = null)
        {

            string inputFolderPath = Path.GetDirectoryName(inputPath) ?? GetOrCreateDefaultFolderPath();
            string newFolderPath = Path.Combine(inputFolderPath, newFolderName);

            Directory.CreateDirectory(newFolderPath); // creates folder if it does not exist

            return newFolderPath;
        }

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
    }
}
