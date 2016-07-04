using System.Collections.Specialized;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace CFAN_factorio_token_fetcher
{
    public class FactorioComAuthClient
    {
        const string BASE_URI = "https://auth.factorio.com/";

        public static string fetchToken(string username, string password)
        {
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.QueryString = new NameValueCollection()
                {
                    {"username", username},
                    {"apiVersion", "2"}
                };
                var response = wc.UploadValues(BASE_URI + "api-login", new NameValueCollection {{"password", password}});
                string jsonString = Encoding.Default.GetString(response);
                var result = JsonConvert.DeserializeObject<string[]>(jsonString);
                return result[0];
            }
        } 
    }
}