using System;
using Newtonsoft.Json;

namespace CKAN.Factorio.Version
{
    [Serializable]
    [JsonConverter(typeof(JsonSimpleStringConverter))]
    public class AutodetectedVersion : AbstractVersion
    {
        private static System.Version createVersionFromString(string versionString)
        {
            try
            {
                return new System.Version(versionString);
            }
            catch
            {
                return null;
            }
        }

        public AutodetectedVersion(string versionString) : base(createVersionFromString(versionString))
        {
        }
    }
}
