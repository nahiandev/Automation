namespace Automation.Helpers
{
    internal class ExternalCredentials
    {
        public static string[] ReadCredentials()
        {
            const int size = 2;
            Console.WriteLine("Enter Username");
            string user = Console.ReadLine()!;
            Console.WriteLine("Enter Password");
            string pass = Console.ReadLine()!;
            return new string[size] { user, pass };
        }

        public static List<string> GetKeywords()
        {
            Console.WriteLine("Enter search keywords seperated with a comma ','.");
            Console.WriteLine("Example: Java, Rust, C++, C#, Python");
            string search_keyword = Console.ReadLine()!;
            return search_keyword.Split(',').ToList();
        }

        public static string[] Messages()
        {
            const int size = 2;
            Console.WriteLine("Enter your invitation message");
            string invitation_message = Console.ReadLine()!;
            Console.WriteLine("Enter your follow up message");
            string follow_up_message = Console.ReadLine()!;
            return new string[size] {invitation_message, follow_up_message };
        }
    }
}