using Newtonsoft.Json.Linq;

namespace Automation.Helpers
{
    internal class JsonToOtherFormats
    {
        private JsonToOtherFormats() { }
        public static List<string> Keywords(string params_file)
        {
            List<string> keywords_list = [];
            var keys = ParamsObject(params_file).GetValue("keywords");
            foreach (var key in keys!)
            {
                keywords_list.Add(key.ToString());
            }

            return keywords_list;
        }
        public static JObject ParamsObject(string params_file)
        {
            using StreamReader params_reader = new(params_file);
            JObject params_object = JObject.Parse(params_reader.ReadToEnd());
            params_reader.Dispose();
            return params_object;
        }
        public static void ToggleFlag(string log_destination, string key, bool toggle)
        {
            using StreamReader json_reader = new(log_destination);
            JObject obsolete_data = JObject.Parse(json_reader.ReadToEnd());
            json_reader.Dispose();

            JObject parsed_data = obsolete_data;

            if (obsolete_data.ContainsKey(key))
            {
                obsolete_data.Remove(key);
                parsed_data.Add(key, toggle);
            }

            WriteFile(log_destination, parsed_data);
        }

        public static void UrlFlagPair(List<string> files, string log_destination)
        {
            const string json_label = "{}";
            DocumentPrototype.CreateRoot(log_destination, json_label);
            foreach (string file in files)
            {
                using StreamReader json_reader = new(file);
                string json_data = json_reader.ReadToEnd();
                json_reader.Dispose();

                if (!string.IsNullOrWhiteSpace(json_data) && json_data != json_label)
                {
                    JObject json_bucket = JObject.Parse(json_data);
                    HashSet<string> unique_container = [];

                    foreach (KeyValuePair<string, JToken?> _json_pair in json_bucket)
                    {
                        string _unique_id = _json_pair.Key;
                        JToken? single_json_chunk = json_bucket[_unique_id];
                        JObject single_json = JObject.Parse(single_json_chunk!.ToString());

                        if (single_json.ContainsKey("linkedin"))
                        {
                            unique_container.Add(single_json.GetValue("linkedin")!.ToString());
                        }
                        else if (single_json.ContainsKey("ceo_linkedin"))
                        {
                            string ceo_value = single_json.GetValue("ceo_linkedin")!.ToString();

                            if (ceo_value != "N/A")
                            {
                                unique_container.Add(ceo_value);
                            }

                            string recruiter_value = single_json.GetValue("recruiter_linkedin")!.ToString();
                            if (recruiter_value != "N/A")
                            {
                                unique_container.Add(recruiter_value);
                            }
                        }
                    }

                    using StreamReader urls_log_file = new(log_destination);
                    JObject obsolate_data = JObject.Parse(urls_log_file.ReadToEnd());
                    urls_log_file.Dispose();
                    JObject parsed_json = obsolate_data;

                    foreach (string url_key in unique_container)
                    {
                        if (!parsed_json.ContainsKey(url_key))
                        {
                            parsed_json.Add(url_key, false);
                        }

                        WriteFile(log_destination, parsed_json);
                    }
                }
            }
        }

        public static IDictionary<string, bool> LogToUrlDictionary(string log_source)
        {
            const string json_label = "{}";
            using StreamReader json_reader = new(log_source);
            string json_data = json_reader.ReadToEnd();
            json_reader.Dispose();

            IDictionary<string, bool> url_flag_pair = new Dictionary<string, bool>();

            if (!string.IsNullOrWhiteSpace(json_data) && json_data != json_label)
            {
                JObject json_bucket = JObject.Parse(json_data);
                foreach (KeyValuePair<string, JToken?> _json_pair in json_bucket)
                {
                    string key = _json_pair.Key;
                    bool flag = Convert.ToBoolean(json_bucket!.GetValue(key)!.ToString().ToLower());
                    url_flag_pair.Add(key, flag);
                }
            }

            return url_flag_pair;
        }
        public static List<string> LogToURLs(string log_source)
        {
            using StreamReader json_reader = new(log_source);
            List<string> profile_urls = [.. json_reader.ReadToEnd().Trim('\n').Split("\r\n")];
            json_reader.Dispose();
            return profile_urls;
        }
        public static bool IsParamsValid(string params_file_path)
        {
            if (!File.Exists(params_file_path)) return false;

            using StreamReader param_reader = new(params_file_path);
            JObject params_object = JObject.Parse(param_reader.ReadToEnd());
            param_reader.Dispose();

            return EvaluateFields(params_object);
        }
        public static void ConvertToCSV(string json_source, string csv_destination, string csv_label)
        {
            DocumentPrototype.CreateRoot(csv_destination, csv_label);
            using StreamReader json_reader = new(json_source);
            JObject json_bucket = JObject.Parse(json_reader.ReadToEnd());
            json_reader.Dispose();

            foreach (KeyValuePair<string, JToken?> _user in json_bucket)
            {
                HashSet<string> container = [];
                string _username = _user.Key;
                JToken? user = json_bucket.GetValue(_username);

                JObject _user_key_values = JObject.Parse(user!.ToString());
                foreach (KeyValuePair<string, JToken?> single_key_value in _user_key_values)
                {
                    string single_property = single_key_value.Key;
                    string associated_value = _user_key_values.GetValue(single_property)!.ToString();
                    container.Add(associated_value);
                }

                string single_row = string.Join(",", container);
                using StreamReader csv_reader = new(csv_destination);
                string[] obsolete_csv = csv_reader.ReadToEnd().Split("\r\n");
                csv_reader.Dispose();

                if (!obsolete_csv.Contains(single_row))
                {
                    WriteFile(csv_destination, single_row);
                }
            }
        }

        private static bool EvaluateFields(JObject params_object)
        {
            return params_object.ContainsKey("keywords")
                && params_object.ContainsKey("invitation_message")
                && params_object.ContainsKey("followup_message")
                && params_object.ContainsKey("search_type")
                && params_object.ContainsKey("location")
                && params_object.GetValue("keywords")!.ToList().Count > 0
                && !string.IsNullOrWhiteSpace(params_object.GetValue("invitation_message")!.ToString())
                && !string.IsNullOrWhiteSpace(params_object.GetValue("followup_message")!.ToString())
                && !string.IsNullOrWhiteSpace(params_object.GetValue("search_type")!.ToString())
                && !string.IsNullOrWhiteSpace(params_object.GetValue("location")!.ToString());
        }

        private static void WriteFile(string destination, string data)
        {
            using StreamWriter file_writer = File.AppendText(destination);
            file_writer.WriteLine(data);
            file_writer.Dispose();
        }

        private static void WriteFile(string destination, JObject data)
        {
            using StreamWriter json_writer = new(destination);
            json_writer.WriteLine(data);
            json_writer.Dispose();
        }
    }
}
