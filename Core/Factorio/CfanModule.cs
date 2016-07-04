using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;

namespace CKAN.Factorio
{
    public class CfanModule : IEquatable<CfanModule>
    {
        protected internal CfanJson cfanJson;

        public ModVersion modVersion => cfanJson.modInfo.version;
        public string identifier => cfanJson.modInfo.name;
        public bool isMetapackage => cfanJson.type == CfanJson.CfanModType.META;
        // @todo: support for mirrors?
        public Uri download => cfanJson.downloadUrls.Any() ? new Uri(cfanJson.downloadUrls.First()) : null;
        public IEnumerable<string> providesNames => new string[0];
        public IEnumerable<ModDependency> depends => cfanJson.modInfo.dependencies.Where(p => p.modName != "base" && !p.isOptional);
        public string[] authors => cfanJson.authors;
        public string description => cfanJson.modInfo.description;
        public CfanJson.CfanModType kind => cfanJson.type;
        public long download_size => cfanJson.downloadSize;
        public string homepage => cfanJson.aggregatorData != null && cfanJson.aggregatorData.ContainsKey("factorio-com-source") ? cfanJson.aggregatorData["factorio-com-source"] : cfanJson.modInfo.homepage;
        public string contact => cfanJson.modInfo.contact;
        public string standardFileName => createStandardFileName(identifier, modVersion.ToString());
        public IEnumerable<ModDependency> recommends => cfanJson.recommends;
        public IEnumerable<ModDependency> suggests => cfanJson.modInfo.dependencies.Where(p => p.modName != "base" && p.isOptional);
        public IEnumerable<ModDependency> conflicts => cfanJson.suggests;
        public IEnumerable<ModDependency> supports => new ModDependency[0];
        public string title => cfanJson.modInfo.title;
        public string @abstract => cfanJson.modInfo.description?.Split('.').FirstOrDefault() ?? "";
        public bool requireFactorioComAuth => CfanJson.requiresFactorioComAuthorization(cfanJson);
        public bool isFromFactorioCom => cfanJson.aggregatorData != null && cfanJson.aggregatorData.ContainsKey("factorio-com-id");
        public string release_status => null;
        public ModDependency BaseGameDependency
            =>
                cfanJson.modInfo.dependencies.FirstOrDefault(p => p.modName == "base") ??
                createDefaultBaseGameDependency();

        public CfanModule(CfanJson cfanJson)
        {
            this.cfanJson = cfanJson;
        }

        public bool ConflictsWith(CfanModule module)
        {
            return false;
        }

        public class NameComparer : IEqualityComparer<CfanModule>
        {
            public bool Equals(CfanModule x, CfanModule y)
            {
                return x.identifier.Equals(y.identifier);
            }

            public int GetHashCode(CfanModule x)
            {
                return x.identifier.GetHashCode();
            }
        }

        public ModDependency createDefaultBaseGameDependency()
        {
            return new ModDependency("base < 0.13");
        }

        public bool IsCompatibleKSP(FactorioVersion kspVersion)
        {
            ModDependency baseGame = BaseGameDependency;
            return baseGame == null || baseGame.isSatisfiedBy("base", kspVersion);
        }

        public FactorioVersion getMinFactorioVersion()
        {
            ModDependency baseGame = BaseGameDependency;
            return baseGame?.minVersion != null ? new FactorioVersion(baseGame.minVersion.ToString()) : null;
        }

        public static string createStandardFileName(string identifier, string version)
        {
            return identifier + "_" + version;
        }

        public AbstractVersion HighestCompatibleKSP()
        {
            ModDependency baseGame = BaseGameDependency;
            return baseGame != null ? new FactorioVersion(baseGame.calculateMaxVersion().ToString()) : null;
        }

        public bool Equals(CfanModule other)
        {
            return other.ToString() == ToString();
        }

        public override string ToString()
        {
            return standardFileName;
        }
    }
}
