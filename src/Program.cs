using Automation.Parser;
using Automation.Helpers;
using Automation.MenuHandlers;
using Newtonsoft.Json.Linq;

namespace Automation
{
    class Program
    {
        static async Task Main()
        {
            Utility.ResetConsole("start");
            Console.WriteLine("Syncing databases...!");
            List<string> source_files = FileGenerator.GetAllFiles("bin", "Models", "json");
            string log_destination = FileGenerator.CreateFileAndCombinePath("bin", "TraceModel", "urls.json");
            string params_file = FileGenerator.CreateFileAndCombinePath("src", "Params", "params.json");
            JsonToOtherFormats.UrlFlagPair(source_files, log_destination);
            Utility.Interrupt(2);
            Console.WriteLine("Done...!");

            const bool headless = false;
            const int timeout = 20;
            int posts_page_fetch_count = new Random().Next(5, 20);

            string search_type_label = "What do you want to search";
            string location_label = "Select country from list";
            string access_label = "Do you want to access previously logged in account";

            int[] inherit_size = [0];
            int[] size = [55, 28];
            string[] search_types = ["people", "jobs", "content"];
            string[] auth_consent = ["yes", "no"];
            string[] credentials = new string[2];

            IDictionary<string, string> locations = Location.AvailableCountries();
            IParser parser;

            string auth_file = FileGenerator.CombineFilePath("bin", "Auth", "session.json");
            bool auth_state = File.Exists(auth_file);

            if (auth_state)
            {
                string auth_type = SelectionMenu.Render(auth_consent, access_label, inherit_size);
                switch (auth_type)
                {
                    case "yes":
                        Console.WriteLine("Accessing previous account.\nAuthenticated. No need Login !");
                        break;

                    case "no":
                        File.Delete(auth_file);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Revoked previously logged in account");
                        Console.ResetColor();
                        credentials = ExternalCredentials.ReadCredentials();
                        break;

                    default:
                        Environment.Exit(0);
                        break;
                }
            }
            else
            {
                credentials = ExternalCredentials.ReadCredentials();
            }

            bool is_valid = JsonToOtherFormats.IsParamsValid(params_file);
            JObject params_object = JsonToOtherFormats.ParamsObject(params_file);

            List<string> keywords_buffer;

            if (is_valid)
            {
                keywords_buffer = JsonToOtherFormats.Keywords(params_file);
            }
            else
            {
                keywords_buffer = ExternalCredentials.GetKeywords();
            }


            string[] deliverable_messages;

            if (is_valid)
            {
                string invitation_message = params_object.GetValue("invitation_message")!.ToString();
                string followup_message = params_object.GetValue("followup_message")!.ToString();
                deliverable_messages = new string[2] { invitation_message, followup_message };
            }
            else
            {
                deliverable_messages = ExternalCredentials.Messages();
            }


            string search_type = string.Empty;


            search_type = is_valid ? params_object.GetValue("search_type")!.ToString() : SelectionMenu.Render(search_types, search_type_label, inherit_size);

            string country_code = string.Empty;

            if (search_type != "content")
            {
                if (is_valid)
                {
                    string country_name = params_object.GetValue("location")!.ToString();
                    country_code = locations[country_name];
                }
                else
                {
                    country_code = SelectionMenu.Render(locations, location_label, size);
                }
                Utility.ResetConsole();
            }

            switch (search_type)
            {
                case "people":
                    parser = new ConnectionParser(auth_file, keywords_buffer, country_code, deliverable_messages[0], deliverable_messages[1], timeout, headless);
                    await parser.Parse(credentials[0], credentials[1]);
                    break;

                case "jobs":
                    parser = new JobParser(auth_file, keywords_buffer, country_code, deliverable_messages[0], deliverable_messages[1], timeout, headless);
                    await parser.Parse(credentials[0], credentials[1]);
                    break;

                case "content":
                    parser = new PostParser(auth_file, keywords_buffer, deliverable_messages[0], deliverable_messages[1], timeout, headless, posts_page_fetch_count);
                    await parser.Parse(credentials[0], credentials[1]);
                    break;

                default:
                    Environment.Exit(0);
                    break;
            }
        }
    }
}
