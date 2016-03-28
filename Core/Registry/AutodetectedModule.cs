using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CKAN.Factorio.Schema;
using Newtonsoft.Json;

namespace CKAN
{
    public class AutodetectedModule
    {
        public AutodetectedModule(string path, ModInfoJson modInfo)
        {
            this.path = path;
            this.modInfo = modInfo;
        }

        [JsonProperty(Required = Required.Always)]
        public string path;

        [JsonProperty(Required = Required.Always)]
        public ModInfoJson modInfo;
    }
}
