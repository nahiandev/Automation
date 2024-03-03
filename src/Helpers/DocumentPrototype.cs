namespace Automation.Helpers
{
    internal class DocumentPrototype
    {
        private DocumentPrototype() {}
        public static void CreateRoot(string file, string label)
        {
            try
            {
                using StreamReader contained_data = new(file);
                var buffer = contained_data.ReadToEnd();
                contained_data.Dispose();

                if (string.IsNullOrWhiteSpace(buffer))
                {
                    using StreamWriter prototype = new(file);
                    prototype.WriteLine(label);
                    prototype.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}