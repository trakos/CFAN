using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFAN_netfan.CfanAggregator.FactorioCom.Schema
{
    public class LatestModReleaseJson
    {
        public class SimpleModInfoJson
        {
            public string factorio_version;
        }

        [JsonProperty(ItemConverterType = typeof(IsoDateTimeConverter))]
        public string version;
        public DateTime? released_at;
        public string download_url;
        public SimpleModInfoJson info_json;
        public string file_name;
    }
}
