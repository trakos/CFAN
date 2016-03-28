using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CKAN.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CKAN.Factorio.Schema
{
    public class ModListJson
    {
        public class ModListJsonItem
        {
            [JsonProperty(Required = Required.Always)]
            public string name;
            [JsonConverter(typeof(JsonEnumDescriptionConverter))]
            [JsonProperty(Required = Required.Always)]
            public ModListJsonTruthy enabled;
        };

        public enum ModListJsonTruthy
        {
            [Description("false")]
            NO = 0,
            [Description("true")]
            YES = 1
        }

        [JsonProperty(Required = Required.Always)]
        public IList<ModListJsonItem> mods;
    }
}
