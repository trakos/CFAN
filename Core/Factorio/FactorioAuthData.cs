using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKAN.Factorio
{
    public class FactorioAuthData
    {
        public string username;
        public string accessToken;

        public static FactorioAuthData parseConfig(string factorioDirectory)
        {
            string configPath = getConfigPath(factorioDirectory);
            if (!File.Exists(configPath))
            {
                throw new NotFactorioDataDirectoryKraken(factorioDirectory, "Cannot find player-data.json");
            }
            string json = File.ReadAllText(configPath);
            dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            if (String.IsNullOrEmpty((string)data["service-token"]) || String.IsNullOrEmpty((string)data["service-username"]))
            {
                throw new NotFactorioDataDirectoryKraken("You have to be logged in Factorio to downloads mods from its mod portal; try checking for updates from in-game first");
            }
            return new FactorioAuthData()
            {
                accessToken = data["service-token"],
                username = data["service-username"]
            };
        }

        public static string getConfigPath(string factorioDirectory)
        {
            return Path.Combine(factorioDirectory, "player-data.json");
        }
    }
}
