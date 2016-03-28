using System;
using Newtonsoft.Json;

namespace CKAN.Factorio.Version
{
    [Serializable]
    [JsonConverter(typeof(JsonSimpleStringConverter))]
    public class NonEmptyVersion : AbstractVersion
    {
        private static System.Version createVersionFromString(string versionString)
        {
            try
            {
                return new System.Version(versionString);
            }
            catch (Exception e)
            {
                throw new BadVersionKraken(versionString, e);
            }
        }

        public NonEmptyVersion(string versionString) : base(createVersionFromString(versionString))
        {
        }
    }
}
