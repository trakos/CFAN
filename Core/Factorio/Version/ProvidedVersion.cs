using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CKAN.Factorio.Version
{
    [Serializable]
    [JsonConverter(typeof(JsonSimpleStringConverter))]
    public class ProvidedVersion : ModVersion
    {
        protected string modProviderIdentifier;

        public ProvidedVersion(string modProviderIdentifier, string versionString) : base(versionString)
        {
            this.modProviderIdentifier = modProviderIdentifier;
        }
    }
}
