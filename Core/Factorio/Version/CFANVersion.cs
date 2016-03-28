using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CKAN.Factorio.Version
{
    [Serializable]
    [JsonConverter(typeof(JsonSimpleStringConverter))]
    public class CFANVersion : NonEmptyVersion
    {
        public string Name { get; protected set; }

        public CFANVersion(string version, string name)
            : base(version)
        {
            Name = name;
        }

        public override string ToString()
        {
            return base.ToString() + " aka " + Name;
        }
    }
}
