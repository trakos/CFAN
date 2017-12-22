using System;
using CKAN.Factorio.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CFAN_netfan.CfanAggregator.FactorioCom.Schema
{
    public class ModReleaseJson
    {
        [JsonProperty(ItemConverterType = typeof(IsoDateTimeConverter))]
        public uint id;
        public string version;
        public string game_version;
        public DateTime? released_at;
        public string download_url;
        public string install_url;
        public ModInfoJson info_json;
        public string file_name;
        public uint file_size;
        public uint downloads_count;
    }
}
