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
                // there is a ridicoulus problem, where:
                //  - mod has a version written as a float instead of string - e.g. 1.3
                //  - local system has locale with decimal comma instead of dot
                //  - conversion from float to string done automagically by JsonSimpleStringConverter changes float 1.3 to string 1,3
                //  - version fails parsing
                versionString = versionString.Replace(',', '.');

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
