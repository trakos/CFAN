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
            var match = Regex.Match(modRequirementString, @"^(?<isOptional>\? )?(?<modName>[a-zA-Z0-9_-][a-zA-Z0-9_ -\.]+[a-zA-Z0-9_-])(?<minVersion> ?>=? ?[0-9\.]+)?(?<maxVersion> ?<=? ?[0-9\.]+)?(?<exactVersion> ?== ?[0-9\.]+)?$");
            if (!match.Success)
            {
                throw new ArgumentException($"Invalid mod requirement string: '{modRequirementString}'", nameof(modRequirementString));
            }
            isOptional = match.Groups["isOptional"].Success;
            modName = match.Groups["modName"].Value;
            if (countTruth(
                match.Groups["minVersion"].Success,
                match.Groups["maxVersion"].Success,
                match.Groups["exactVersion"].Success
                ) > 1)
            {
                throw new ArgumentException($"Invalid mod requirement string: '{modRequirementString}'", nameof(modRequirementString));
            }
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
                minVersion = maxVersion = new ModVersion(match.Groups["exactVersion"].Value.Replace("==", "").Trim());
            }
        }

        public bool isSatisfiedBy(string name, AbstractVersion modVersion)
        {
            if (name != modName)
            {
                return isOptional;
            }
            return (minVersion == null || modVersion >= minVersion) && (maxVersion == null || modVersion <= maxVersion);
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
            else if (minVersion != null && maxVersion == null)
            {
                modRequirementStringBuilder.Append(" >= ");
                modRequirementStringBuilder.Append(minVersion);
            }
            else if (maxVersion != null && minVersion == null)
            {
                modRequirementStringBuilder.Append(" <= ");
                modRequirementStringBuilder.Append(maxVersion);
            }
            // both are set, and are not equals
            else if (maxVersion != null && minVersion != null)
            {
                // @todo: added as an afterthought, prolly gonna cause havoc
                throw new Exception("Unsupported version dependency combination");
            }
            return modRequirementStringBuilder.ToString();
        }
    }
}
