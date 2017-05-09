using System;
using Newtonsoft.Json;

namespace CKAN.Factorio.Version
{
    [Serializable]
    [JsonConverter(typeof(JsonSimpleStringConverter))]
    public class ModVersion : NonEmptyVersion
    {
        private AbstractVersion maxVersion;

        public ModVersion(string versionString) : base(versionString)
        {
        }

        public ModVersion(AbstractVersion version) : base(version.ToString())
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
            if (minVersion.version.Build <= 0)
            {
                if (minVersion.version.Minor <= 0)
                {
                    if (minVersion.version.Major <= 0)
                    {
                        // @todo: check whether this can break something
                        return new ModVersion(new System.Version(0, 0, 0).ToString());
                    }
                    return new ModVersion(new System.Version(minVersion.version.Major - 1, int.MaxValue, int.MaxValue).ToString());
                }
                return
                    new ModVersion(
                        new System.Version(minVersion.version.Major, minVersion.version.Minor - 1, int.MaxValue)
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

        // that's silly, but should work
        public static bool isMaxWithTheSameMinor(ModVersion minVersion)
        {
            return minVersion.ToString() ==
                   ModVersion.maxWithTheSameMinor(minVersion).ToString();
        }

        // that's silly, but should work
        public static ModVersion incrementMinorVersion(ModVersion minVersion)
        {
            return
                new ModVersion(
                    new System.Version(minVersion.version.Major, minVersion.version.Minor + 1, 0)
                        .ToString()
                    );
        }
    }
}
