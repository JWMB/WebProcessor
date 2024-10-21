namespace NoK.Tests
{
    internal class Helpers
    {
        public static string GetJsonFile(string filename)
        {
            //var folder = @"C:\Users\jonas\Downloads\NoK";
            //var pathToFile = @"C:\Users\jonas\Downloads\assignments_141094_16961\assignments_141094_16961.json";
            var directory = new DirectoryInfo(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "NoK"));
            if (directory.Exists)
            {
                var file = directory.GetFiles(filename).FirstOrDefault(); // "assignments_141094_16961.json"
                if (file?.Exists == true)
                    return file.FullName;
            }
            throw new FileNotFoundException(filename);
        }
    }
}
