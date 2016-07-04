using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CKAN.Converters;
using CKAN.Factorio.Relationships;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CKAN.Factorio.Schema
{
    public class CfanJson
    {
        [JsonProperty(Required = Required.Always)]
        public ModInfoJson modInfo;

        [JsonProperty(Required = Required.Always)]
        public string[] authors;

        [JsonProperty(Required = Required.Always)]
        public string[] categories;

        [JsonProperty(Required = Required.Always)]
        public string[] tags;

        [JsonProperty(Required = Required.Always)]
        public ModDependency[] suggests;

        [JsonProperty(Required = Required.Always)]
        public ModDependency[] recommends;

        [JsonProperty(Required = Required.Always)]
        public ModDependency[] conflicts;

        [JsonProperty(Required = Required.Always)]
        public string[] downloadUrls;

        [JsonProperty(Required = Required.AllowNull)]
        public long downloadSize;

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(JsonEnumDescriptionConverter))]
        public CfanModType type;

        [JsonProperty(ItemConverterType = typeof(IsoDateTimeConverter), Required = Required.AllowNull)]
        public DateTime? releasedAt;

        [JsonProperty(Required = Required.Always)]
        public IDictionary<string, string> aggregatorData;

        public enum CfanModType
        {
            MOD = 1,
            TEXTURES = 2,
            META = 3,
            SCENARIO = 4
        }

        public static bool requiresFactorioComAuthorization(CfanJson cfanJson)
        {
            return cfanJson.aggregatorData != null &&
                   cfanJson.aggregatorData.ContainsKey("requires-factorio-token") &&
                   cfanJson.aggregatorData["requires-factorio-token"] == "1";
        }
    }
}