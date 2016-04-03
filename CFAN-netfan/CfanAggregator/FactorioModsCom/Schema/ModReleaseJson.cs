using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom.Schema
{
    class ModReleaseJson
    {
        [JsonProperty(ItemConverterType = typeof(IsoDateTimeConverter))]
        public int id;
        public string version;
        public DateTime? released_at;
        public string[] game_versions;
        public int[] dependencies;
        public ModReleaseJsonFile[] files;

        public class ModReleaseJsonFile
        {
            public int id;
            public string name;
            public string mirror;
            public string url;
        }
    }
}
