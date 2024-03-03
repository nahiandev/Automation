using Automation.Helpers;
using Newtonsoft.Json.Linq;

namespace Automation.RecordHandlers
{
    internal class ConnectionRecords
    {
        public static void SaveRecords(string record_file, string json_label, string username, string search_param, string name, string designation, string company, string website, string phone, string email, string linkedin, 
        string twitter, bool flag)
        {
            DocumentPrototype.CreateRoot(record_file, json_label);
            using StreamReader json_reader = new(record_file);
            JObject obsolete_json = JObject.Parse(json_reader.ReadToEnd());
            json_reader.Dispose();
            JObject parsed_json = obsolete_json;
            
            if (!parsed_json.ContainsKey(username))
            {
                JObject items = new()
                           {
                               { "keyword", search_param},
                               { "name",  name},
                               {"designation", Utility.Clear(designation)},
                               {"company", Utility.Clear(company)},
                               {"website", Utility.Clear(website).Split(' ')[0]},
                               {"phone", Utility.Clear(phone)},
                               {"email", Utility.Clear(email)},
                               {"linkedin", linkedin},
                               {"twitter", Utility.Clear(twitter)},
                               {"message_sent", flag}
                           };

                parsed_json.Add(username, items);
            }
            using StreamWriter json_writer = new(record_file);
            json_writer.WriteLine(parsed_json);
            json_writer.Dispose();
        }
    }
}