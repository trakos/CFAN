using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CKAN.Factorio.Version;

namespace CKAN.Factorio
{
    public class CfanModuleIdAndVersion
    {
        public string identifier { get; protected set; }
        public AbstractVersion version { get; protected set; }

        public CfanModuleIdAndVersion(string identifier, AbstractVersion version)
        {
            this.identifier = identifier;
            this.version = version;
        }

        public CfanModuleIdAndVersion(string stringWithIdentifierEqualsVersion)
        {
            Match match = Regex.Match(stringWithIdentifierEqualsVersion,
                @"^(?<mod>[A-Za-z0-9-_]*)(?<version>=\d+\.\d+\.?\d*)?$");

            if (!match.Success)
            {
                throw new ModuleAndVersionStringInvalidKraken(stringWithIdentifierEqualsVersion);
            }

            identifier = match.Groups["mod"].Value;
            if (match.Groups["version"].Success)
            {
                version = new ModVersion(match.Groups["version"].Value.TrimStart('='));
            }
        }
    }
}
