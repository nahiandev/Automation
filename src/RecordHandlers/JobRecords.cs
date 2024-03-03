using Automation.Helpers;
using Newtonsoft.Json.Linq;

namespace Automation.RecordHandlers
{
    internal class JobRecords
    {
        public static void SaveRecords(string record_file, string json_label, string job_id, string job_title, string job_type, string job_location, string job_url, string company_name, string company_website, string company_size, string recruiter_name,
        string recruiter_linkedin, string ceo_name, string ceo_linkedin)
        {
            DocumentPrototype.CreateRoot(record_file, json_label);
            using StreamReader json_reader = new(record_file);
            JObject obsolete_json = JObject.Parse(json_reader.ReadToEnd());
            json_reader.Dispose();
            JObject parsed_json = obsolete_json;

            if (!parsed_json.ContainsKey(job_id))
            {
                JObject items = new()
                           {
                               
                               { "job_title",  job_title},
                               {"job_type", job_type},
                               {"job_location", job_location},
                               {"job_url", job_url},
                               {"company_name", company_name},
                               {"company_website", company_website},
                               {"company_size", company_size},
                               {"recruiter_name", recruiter_name},
                               {"recruiter_linkedin", recruiter_linkedin},
                               {"ceo_name", ceo_name},
                               {"ceo_linkedin", ceo_linkedin}
                           };

                parsed_json.Add(job_id, items);
            }
            using StreamWriter json_writer = new(record_file);
            json_writer.WriteLine(parsed_json);
            json_writer.Dispose();
        }
    }
}
