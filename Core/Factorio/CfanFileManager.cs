using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CKAN.Factorio.Schema;
using Newtonsoft.Json;

namespace CKAN.Factorio
{
    public static class CfanFileManager
    {
        public static CfanModule fromCfanFile(string filename)
        {
            return fromJson(File.ReadAllText(filename));
        }

        public static void toCfanFile(this CfanModule module, string filename)
        {
            File.WriteAllText(filename, toJson(module));
        }

        public static CfanModule fromJson(string json)
        {
            return new CfanModule(JsonConvert.DeserializeObject<CfanJson>(json));
        }

        public static string toJson(this CfanModule cfanJson)
        {
            return JsonConvert.SerializeObject(cfanJson.cfanJson);
        }
    }
}
