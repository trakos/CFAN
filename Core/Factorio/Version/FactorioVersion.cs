using System;
using Newtonsoft.Json;

namespace CKAN.Factorio.Version
{
    [Serializable]
    [JsonConverter(typeof(JsonSimpleStringConverter))]
    public class FactorioVersion : NonEmptyVersion
    {
        public FactorioVersion(string versionString) : base(versionString)
        {
        }
    }
}
