using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CKAN.Factorio.Version;
using Newtonsoft.Json;

namespace CKAN.Factorio.Relationships
{
    [Serializable]
    [JsonConverter(typeof(JsonSimpleStringConverter))]
    public class ModDependency
    {
        public ModVersion minVersion { get; protected set; }
        public ModVersion maxVersion { get; protected set; }
        public string modName { get; protected set; }
        public bool isOptional { get; protected set; }

        private static int countTruth(params bool[] booleans)
        {
            return booleans.Count(b => b);
        }

        public ModDependency(string modRequirementString)
        {
            var match = Regex.Match(modRequirementString, @"^(?<isOptional>\? ?)?(?<modName>[a-zA-Z0-9_-][a-zA-Z0-9_ -\.]+[a-zA-Z0-9_-])(?<notEqualVersion> *!= *[0-9\.]+)?(?<minVersion> *>=? *[0-9\.]+)?(?<maxVersion> *<=? *[0-9\.]+)?(?<exactVersion> *==? *[0-9\.]+)?$");
            if (!match.Success)
            {
                throw new ArgumentException($"Invalid mod requirement string: '{modRequirementString}'", nameof(modRequirementString));
            }
            isOptional = match.Groups["isOptional"].Success;
            modName = match.Groups["modName"].Value;
            if (match.Groups["minVersion"].Success)
            {
                if (!modRequirementString.Contains(">="))
                {
                    minVersion = new ModVersion(match.Groups["minVersion"].Value.Replace(">", "").Trim());
                    minVersion = ModVersion.increment(minVersion);
                }
                else
                {
                    minVersion = new ModVersion(match.Groups["minVersion"].Value.Replace(">=", "").Trim());
                }
            }
            if (match.Groups["maxVersion"].Success)
            {
                if (!modRequirementString.Contains("<="))
                {
                    maxVersion = new ModVersion(match.Groups["maxVersion"].Value.Replace("<", "").Trim());
                    maxVersion = ModVersion.decrement(maxVersion);
                }
                else
                {
                    maxVersion = new ModVersion(match.Groups["maxVersion"].Value.Replace("<=", "").Trim());
                }
            }
            if (match.Groups["exactVersion"].Success)
            {
                minVersion = maxVersion = new ModVersion(match.Groups["exactVersion"].Value.Replace("=", "").Trim());
            }
            // @todo: it should allow versions lower than this, but it's not implemented yet
            if (match.Groups["notEqualVersion"].Success)
            {
                minVersion = new ModVersion(match.Groups["notEqualVersion"].Value.Replace("!=", "").Trim());
                minVersion = ModVersion.increment(minVersion);
            }
        }

        public ModDependency(ModVersion minVersion, ModVersion maxVersion, string modName, bool isOptional)
        {
            this.minVersion = minVersion;
            this.maxVersion = maxVersion;
            this.modName = modName;
            this.isOptional = isOptional;
        }

        public ModVersion calculateMaxVersion()
        {
            if (modName == "base" && maxVersion == null)
            {
                // we assume that mods without no game version specified are only valid for versions below 0.13
                return minVersion == null ? ModVersion.maxWithTheSameMinor(new ModVersion("0.12")) : ModVersion.maxWithTheSameMinor(minVersion);
            }
            return maxVersion;
        }

        public bool isSatisfiedBy(string name, AbstractVersion modVersion)
        {
            if (name != modName)
            {
                return isOptional;
            }
            var calculatedMaxVersion = calculateMaxVersion();
            return (minVersion == null || modVersion >= minVersion) && (calculatedMaxVersion == null || modVersion <= calculatedMaxVersion);
        }

        public bool isSatisfiedBy(Dictionary<string, AbstractVersion> modVersions)
        {
            if (!modVersions.ContainsKey(modName))
            {
                return isOptional;
            }
            return isSatisfiedBy(modName, modVersions[modName]);
        }

        public override string ToString()
        {
            StringBuilder modRequirementStringBuilder = new StringBuilder();
            if (isOptional)
            {
                modRequirementStringBuilder.Append("? ");
            }
            modRequirementStringBuilder.Append(modName);
            if (minVersion == maxVersion && minVersion != null)
            {
                modRequirementStringBuilder.Append(" == ");
                modRequirementStringBuilder.Append(minVersion);
            }
            else
            {
                if (minVersion != null)
                {
                    modRequirementStringBuilder.Append(" >= ");
                    modRequirementStringBuilder.Append(minVersion);
                }
                if (maxVersion != null)
                {
                    modRequirementStringBuilder.Append(" <= ");
                    modRequirementStringBuilder.Append(maxVersion);
                }
            }
            return modRequirementStringBuilder.ToString();
        }
    }
}
