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
    public class CfanModule
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
        public string homepage => cfanJson.modInfo.homepage;
        public string contact => cfanJson.modInfo.contact;
        public string standardFileName => createStandardFileName(identifier, modVersion.ToString());
        public IEnumerable<ModDependency> recommends => cfanJson.recommends;
        public IEnumerable<ModDependency> suggests => cfanJson.modInfo.dependencies.Where(p => p.modName != "base" && p.isOptional);
        public IEnumerable<ModDependency> conflicts => cfanJson.suggests;
        public IEnumerable<ModDependency> supports => new ModDependency[0];
        public string title => cfanJson.modInfo.title;
        public string @abstract => cfanJson.modInfo.description?.Split('.').FirstOrDefault() ?? "";
        public string release_status => null;

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

        public bool IsCompatibleKSP(FactorioVersion kspVersion)
        {
            ModDependency baseGame = cfanJson.modInfo.dependencies.FirstOrDefault(p => p.modName == "base");
            return baseGame?.minVersion == null || baseGame.minVersion <= kspVersion;
        }

        public FactorioVersion getMinFactorioVersion()
        {
            ModDependency baseGame = cfanJson.modInfo.dependencies.FirstOrDefault(p => p.modName == "base");
            return baseGame?.minVersion != null ? new FactorioVersion(baseGame.minVersion.ToString()) : null;
        }

        public static string createStandardFileName(string identifier, string version)
        {
            return identifier + "_" + version;
        }

        public AbstractVersion HighestCompatibleKSP()
        {
            ModDependency baseGame = cfanJson.modInfo.dependencies.FirstOrDefault(p => p.modName == "base");
            return baseGame?.maxVersion != null ? new FactorioVersion(baseGame.maxVersion.ToString()) : null;
        }

        public override string ToString()
        {
            return standardFileName;
        }
    }
}
