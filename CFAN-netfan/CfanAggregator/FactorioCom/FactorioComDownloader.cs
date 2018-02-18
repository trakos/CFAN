using System;
using System.Collections.Generic;
using System.Linq;
using CFAN_netfan.CfanAggregator.Aggregators;
using CFAN_netfan.CfanAggregator.FactorioCom.Schema;
using CKAN;
using CKAN.Factorio;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;

namespace CFAN_netfan.CfanAggregator.FactorioCom
{
    public class FactorioComDownloader
    {
        public IEnumerable<CfanJson> generateCfanJsons(IUser user, ModJson modJson)
        {
            CfanJson cfanJson = getCfanJson(user, modJson, modJson.latest_release);
            return cfanJson == null ? new CfanJson[0] : new[] { cfanJson };
        }

        protected CfanJson getCfanJson(IUser user, ModJson modJson, LatestModReleaseJson latestModReleaseJson)
        {
            if (string.IsNullOrEmpty(latestModReleaseJson.download_url))
            {
                user.RaiseError($"Mod {modJson.name} does not have download url, omitting");
                return null;
            }
            ModInfoJson infoJson = new ModInfoJson
            {
                factorio_version = latestModReleaseJson.info_json.factorio_version,
                author = new List<string> { modJson.owner },
                name = modJson.name,
                title = modJson.title,
                homepage = modJson.homepage,
                contact = modJson.github_path,
                version = new ModVersion(latestModReleaseJson.version),
                description = modJson.summary,
                dependencies = new ModDependency[] { }
            };
            fixBaseGameVersionRequirement(infoJson);
            var cfanJson = CfanGenerator.createCfanJsonFromModInfoJson(infoJson, 0);
            cfanJson.downloadUrls = new[] { FactorioComAggregator.BASE_URI + latestModReleaseJson.download_url};
            cfanJson.aggregatorData = new Dictionary<string, string>
            {
                ["x-source"] = typeof (FactorioComAggregator).Name,
                ["factorio-com-id"] = modJson.id.ToString(),
                ["factorio-com-source"] = FactorioComAggregator.BASE_URI + "/mods/" + modJson.owner + "/" + modJson.name,
                ["requires-factorio-token"] = "1"
            };
            cfanJson.tags = new string[] { };
            cfanJson.categories = new string[] { };
            return cfanJson;
        }

        protected void fixBaseGameVersionRequirement(ModInfoJson modInfoJson)
        {
            // cfan does not allow empty base game version requirement for versions above 0.13
            var baseGameRequirement = modInfoJson.dependencies.FirstOrDefault(p => p.modName == "base");
            if (String.IsNullOrEmpty(modInfoJson.factorio_version))
            {
                throw new Exception("Mod " + modInfoJson.name + " has empty factorio_version field.");
            }
            ModVersion minVersion = ModVersion.minWithTheSameMinor(new ModVersion(modInfoJson.factorio_version));
            ModVersion maxVersion =  ModVersion.maxWithTheSameMinor(new ModVersion(modInfoJson.factorio_version));
            var newBaseDependency = new ModDependency(minVersion, maxVersion, "base", false);

            // add new or substitute existing base dependency 
            var newDependencies = modInfoJson.dependencies.Where(p => p != baseGameRequirement).ToList();
            newDependencies.Add(newBaseDependency);
            modInfoJson.dependencies = newDependencies.ToArray();
        }
    }
}
