using System;
using CKAN.Factorio.Version;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CFAN_netfan.CfanAggregator.FactorioCom.Schema
{
    public class ModJson
    {
        [JsonProperty(ItemConverterType = typeof(IsoDateTimeConverter))]
        public string license_url;
        public string name;
        public string homepage;
        public uint license_flags;
        public string github_path;
        public ModTagJson[] tags;
        public uint downloads_count;
        public double? current_user_rating;
        public string title;
        public string summary;
        public string license_name;
        public DateTime? updated_at;
        public FactorioVersion[] game_versions;
        public uint ratings_count;
        public string owner;
        public DateTime? created_at;
        public ModMediaFileJson first_media_file;
        public uint id;
        [JsonProperty(Required = Required.Always)]
        public ModReleaseJson latest_release;

        public class ModTagJson
        {
            public uint id;
            public string name;
            public string title;
            public string description;
            public char type;
        }
    }
}
