using System;
using Newtonsoft.Json;

namespace CKAN.Factorio.Version
{
    [Serializable]
    [JsonConverter(typeof(JsonSimpleStringConverter))]
    public class ModVersion : NonEmptyVersion
    {
        public ModVersion(string versionString) : base(versionString)
        {
        }

        // When cast from a string.
        public static explicit operator ModVersion(string v)
        {
            return new ModVersion(v);
        }

        // that's silly, but should work
        public static ModVersion increment(ModVersion minVersion)
        {
            return
                new ModVersion(
                    new System.Version(minVersion.version.Major, minVersion.version.Minor, minVersion.version.Build + 1)
                        .ToString()
                    );
        }

        // that's silly, but should work
        public static ModVersion decrement(ModVersion minVersion)
        {
            if (minVersion.version.Build == 0)
            {
                return
                    new ModVersion(
                        new System.Version(minVersion.version.Major, minVersion.version.Minor)
                            .ToString()
                        );
            }
            return
                new ModVersion(
                    new System.Version(minVersion.version.Major, minVersion.version.Minor, minVersion.version.Build - 1)
                        .ToString()
                    );
        }

        // that's silly, but should work
        public static ModVersion minWithTheSameMinor(ModVersion minVersion)
        {
            return
                new ModVersion(
                    new System.Version(minVersion.version.Major, minVersion.version.Minor, 0)
                        .ToString()
                    );
        }

        // that's silly, but should work
        public static ModVersion maxWithTheSameMinor(ModVersion minVersion)
        {
            return
                new ModVersion(
                    new System.Version(minVersion.version.Major, minVersion.version.Minor, int.MaxValue)
                        .ToString()
                    );
        }
    }
}
