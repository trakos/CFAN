using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Version;
using Newtonsoft.Json;

namespace CKAN.Factorio.Schema
{
    public class ModInfoJson
    {
        [JsonProperty(Required = Required.Always)]
        public string name;

        [JsonProperty(Required = Required.Always)]
        public ModVersion version;

        public FactorioVersion factorio_version;

        [JsonProperty(Required = Required.Always)]
        public string title;

        [JsonProperty(Required = Required.Always)]
        public string author;

        public string contact;
        public string homepage;
        public string description;
        public ModDependency[] dependencies;

        [OnDeserialized]
        public void removeNulls(StreamingContext streamingContext)
        {
            if (dependencies == null)
            {
                dependencies = new ModDependency[0];
            }
        }
    }
}
