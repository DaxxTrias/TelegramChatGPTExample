using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CloysterGPT
{
    public class UrbanDictionary
    {
        // Disable the warning.
#pragma warning disable SYSLIB0014
        private static string Http(string endpoint)
        {
            //todo: re-do this as asyn httpclient (chatgpt has the example in #coding mar-26-23)
            try
            {
                HttpWebRequest client = (HttpWebRequest)WebRequest.Create($"https://api.urbandictionary.com/v0/{endpoint}");
                client.Method = "GET";
                var webResponse = client.GetResponse();
                var webStream = webResponse.GetResponseStream();
                var responseReader = new System.IO.StreamReader(webStream);
                var response = responseReader.ReadToEnd();

                // dispose it when we're dne
                responseReader.Close();
                responseReader.Dispose();

                return response;
            }
            catch (Exception)
            {
                return null;
            }
        }
        // Re-enable the warning.
#pragma warning restore SYSLIB0014

        public static List<Response> Search(string input)
        {
            try
            {
                string text = Http($"define?term={Uri.EscapeDataString(input)}");

                if (string.IsNullOrWhiteSpace(text))
                    return null;

                BaseResponse baseResponse = JsonConvert.DeserializeObject<BaseResponse>(text);

                if (baseResponse?.list?.Count == 0)
                    return null;

                return new List<Response>(baseResponse.list);
            }
            catch (JsonException)
            {
                //todo: handle it
                return null;
            }
        }

        public static Response DefinitionById(long id)
        {
            try
            {
                var response = Http($"define?defid={id}");

                if (response != null && response.Length != 0)
                {
                    BaseResponse data = JsonConvert.DeserializeObject<BaseResponse>(response);

                    if (data.list != null && data.list.Count != 0)
                    {
                        return data.list[0];
                    }
                    else
                        return null;
                }
                else
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
    public class Response
    {
        public string definition { get; set; }
        public string permalink { get; set; }
        public int thumbs_up { get; set; }
        public List<string> sound_urls { get; set; }
        public string author { get; set; }
        public string word { get; set; }
        public int defid { get; set; }
        public string current_vote { get; set; }
        public string written_on { get; set; }
        public string example { get; set; }
        public int thumbs_down { get; set; }
    }
    public class BaseResponse
    {
        public List<Response> list;
    }
}
