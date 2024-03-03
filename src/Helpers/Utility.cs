using System.Security.Principal;
using System.Web;

namespace Automation.Helpers
{
    internal class Utility
    {
        private const int ConsoleWidth = 66;
        private const int ConsoleHeight = 17;
        private const int SleepDuration = 2500;

        private Utility() { }

        public static int MinToMiliSec(int min) => min * 60 * 1000;

        public static void ResetConsole()
        {
            try
            {
                Console.SetWindowSize(ConsoleWidth, ConsoleHeight);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Welcome onboard. Wait till we initialize ...!");
                Console.ResetColor();
            }
        }

        public static async Task ResetConsole(string start)
        {
            try
            {
                Console.SetWindowSize(ConsoleWidth, ConsoleHeight);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                string username = WindowsIdentity.GetCurrent().Name.Split('\\')[1];
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"Hey {username}, We're happy to have you ...!");
                Console.ResetColor();
            }
            await Task.Delay(SleepDuration);
        }

        public static string CustomizesMessage(string name, string message)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            }

            string firstName = name.Split(' ')[0];
            int greetingEndIndex = message.IndexOf(' ');
            string greeting = message.Substring(0, greetingEndIndex);
            string messageBody = message.Substring(greetingEndIndex + 1);
            string customizedMessage = $"{greeting} {firstName},\n{messageBody}";

            return customizedMessage;
        }

        public static string EncodeKeyword(string keyword) => HttpUtility.UrlEncode(keyword);

        public static async Task Interrupt()
        {
            int interval = new Random().Next(2, 4) * 1000;
            await Task.Delay(interval);
        }

        public static async Task Interrupt(int interval)
        {
            int time = interval * 1000;
            await Task.Delay(time);
        }

        public static string Clear(string text) => text.Replace('\n', ' ').Trim();
    }
}
