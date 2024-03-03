namespace Automation.Helpers
{
    internal class FileGenerator
    {
        private FileGenerator() {}
        public static string CreateFileAndCombinePath(string place_before, string sub_dir, string file_name)
        {
            string path = PathUpto(place_before, sub_dir);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string file = Path.Combine(path, file_name);
            if (!File.Exists(file))
            {
                File.Create(file).Dispose();
            }

            return file;
        }

        public static List<string> GetAllFiles(string place_before, string sub_dir, string extension)
        {
            string path = PathUpto(place_before, sub_dir);

            DirectoryInfo file_directory = new(path);
            FileInfo[] files = file_directory.GetFiles($"*.{extension}");

            List<string> files_buffer = [];
            foreach (var item in files)
            {
                files_buffer.Add(item.ToString());
            }

            return files_buffer;
        }

        public static string CombineFilePath(string place_before, string sub_dir, string file_name)
        {
            string path = PathUpto(place_before, sub_dir);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string full_path = Path.Combine(path, file_name);
            return full_path;
        }

        private static string PathUpto(string place_before, string sub_dir)
        {
            string path = Directory.GetCurrentDirectory().ToString().Split(place_before)[0];
            path = Path.Combine(path, sub_dir);
            return path;
        }
    }
}
