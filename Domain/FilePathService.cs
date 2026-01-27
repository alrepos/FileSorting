namespace Domain
{
    public class FilePathService
    {
        public string GetNewFilePath(string? fileFolder = null)
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

        public string GetOrCreateDefaultFolderPath()
        {
            string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            string defaultFolder = Path.Combine(appFolder, "FileSorting");

            Directory.CreateDirectory(defaultFolder); // creates folder if it does not exist

            return defaultFolder;
        }

        public string GetOrCreateNewFolderPath(string newFolderName, string? inputPath = null)
        {

            string inputFolderPath = Path.GetDirectoryName(inputPath) ?? GetOrCreateDefaultFolderPath();
            string newFolderPath = Path.Combine(inputFolderPath, newFolderName);

            Directory.CreateDirectory(newFolderPath); // creates folder if it does not exist

            return newFolderPath;
        }

        public string GetDefaultInputFilePath()
        {
            return GetDefaultFilePath("generated_file.txt");
        }

        public string GetDefaultOutputFilePath()
        {
            return GetDefaultFilePath("sorted_file.txt");
        }

        private string GetDefaultFilePath(string fileName)
        {
            string defaultFolder = GetOrCreateDefaultFolderPath();

            string defaultFilePath = Path.Combine(defaultFolder, fileName);
            return defaultFilePath;
        }
    }
}
