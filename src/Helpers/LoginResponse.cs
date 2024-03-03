namespace Automation.Helpers
{
    internal class LoginResponse
    {
        public static void FailedResponse()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Wrong credentials. Double check you username and password.");
            Thread.Sleep(5000);
            Console.ResetColor();
        }

        public static void AttemptLabel()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Trying to login ...!");
            Console.ResetColor();
        }

        public static void AttemptSuccess()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Logged in successfully ...!");
            Console.ResetColor();
        }
    }
}