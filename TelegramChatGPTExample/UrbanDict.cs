using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace CloysterGPT
{
    public class UrbanDictionary
    {
        // Disable the warning. https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib0014
#pragma warning disable SYSLIB0014
        private static string Http(string endpoint)
        {
            //todo: re-do this as async httpclient (chatgpt has the example in #coding mar-26-23)
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
            catch (WebException ex)
            {
                Utils.WriteLine("WebException ocurred" + ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Utils.WriteLine("Exception ocurred" + ex.Message);
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
            catch (JsonException ex)
            {
                Utils.WriteLine("Exception occurred: " + ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Utils.WriteLine("Exception occurred: " + ex.Message);
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
        public string definition { get; private set; }
        public string permalink { get; private set; }
        public int thumbs_up { get; private set; }
        public List<string> sound_urls { get; private set; }
        public string author { get; private set; }
        public string word { get; private set; }
        public int defid { get; private set; }
        public string current_vote { get; private set; }
        public string written_on { get; private set; }
        public string example { get; private set; }
        public int thumbs_down { get; private set; }
    }
    public class BaseResponse
    {
        public List<Response> list;
    }
}
