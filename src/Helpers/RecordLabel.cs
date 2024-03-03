namespace Automation.Helpers
{
    internal class RecordLabel
    {
        public static void WritingDatabase()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("=> Writing to database");
            Console.ResetColor();
        }
        public static void RecordAdded()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("=> Record added");
            Console.ResetColor();
        }
    }
}
