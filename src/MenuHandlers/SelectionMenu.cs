namespace Automation.MenuHandlers
{
    internal class SelectionMenu
    {
        public static string Render(IDictionary<string, string> menu_items, string menu_label, int[] render_size)
        {
            
            string result = string.Empty;
            if (render_size.Length == 2)
            {
                Console.SetWindowSize(render_size[0], render_size[1]);
            }

            List<Option> options = new();
            foreach (KeyValuePair<string, string> item in menu_items)
            {
                string key = item.Key;
                Option key_value = new($"{key}", () => result = menu_items[key].ToLower());
                options.Add(key_value);
            }

            int index = 0;
            WriteMenu(options, options[index], menu_label);

            ConsoleKeyInfo keyinfo;
            do
            {
                keyinfo = Console.ReadKey();
                if (keyinfo.Key == ConsoleKey.DownArrow)
                {
                    if (index + 1 < options.Count)
                    {
                        index++;
                        WriteMenu(options, options[index], menu_label);
                    }
                }
                if (keyinfo.Key == ConsoleKey.UpArrow)
                {
                    if (index - 1 >= 0)
                    {
                        index--;
                        WriteMenu(options, options[index], menu_label);
                    }
                }
                if (keyinfo.Key == ConsoleKey.Enter)
                {
                    options[index].Selected.Invoke();
                    break;
                }
            } while (keyinfo.Key != ConsoleKey.X);

            return result;
        }
        public static string Render(string[] menu_items, string menu_label, int[] render_size)
        { 
            string result = string.Empty;
            if (render_size.Length == 2)
            {
                Console.SetWindowSize(render_size[0], render_size[1]);
            }

            List<Option> options = new();
            foreach (string item in menu_items)
            {
                Option key_value = new($"{item[..1].ToUpper()}{item[1..].ToLower()}", () => result = item.ToLower());
                options.Add(key_value);
            }

            int index = 0;
            WriteMenu(options, options[index], menu_label);

            ConsoleKeyInfo keyinfo;
            do
            {
                keyinfo = Console.ReadKey();
                if (keyinfo.Key == ConsoleKey.DownArrow)
                {
                    if (index + 1 < options.Count)
                    {
                        index++;
                        WriteMenu(options, options[index], menu_label);
                    }
                }
                if (keyinfo.Key == ConsoleKey.UpArrow)
                {
                    if (index - 1 >= 0)
                    {
                        index--;
                        WriteMenu(options, options[index], menu_label);
                    }
                }
                if (keyinfo.Key == ConsoleKey.Enter)
                {
                    options[index].Selected.Invoke();
                    break;
                }
            } while (keyinfo.Key != ConsoleKey.X);

            return result;
        }
        private static void WriteMenu(List<Option> options, Option selected_option, string menu_label)
        {
            Console.Clear();
            Console.WriteLine($"{menu_label}?\nSelect using Up & Down arrow keys and press Enter.");
            foreach (Option option in options)
            {
                if (option == selected_option)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("-> ");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(" ");
                }
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine(option.Name);
                Console.ResetColor();
            }
        }
    }
}