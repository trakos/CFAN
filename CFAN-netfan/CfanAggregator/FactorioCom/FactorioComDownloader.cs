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

        protected CfanJson getCfanJson(IUser user, ModJson modJson, ModReleaseJson modReleaseJson)
        {
            if (string.IsNullOrEmpty(modReleaseJson.download_url))
            {
                user.RaiseError($"Mod {modJson.name} does not have download url, omitting");
                return null;
            }
            fixBaseGameVersionRequirement(modReleaseJson.info_json);
            var cfanJson = CfanGenerator.createCfanJsonFromModInfoJson(modReleaseJson.info_json, modReleaseJson.file_size);
            cfanJson.downloadUrls = new[] { FactorioComAggregator.BASE_URI + modReleaseJson.download_url};
            cfanJson.aggregatorData = new Dictionary<string, string>
            {
                ["x-source"] = typeof (FactorioComAggregator).Name,
                ["factorio-com-id"] = modJson.id.ToString(),
                ["factorio-com-source"] = FactorioComAggregator.BASE_URI + "/mods/" + modJson.owner + "/" + modJson.name,
                ["requires-factorio-token"] = "1"
            };
            cfanJson.tags = modJson.tags.Select(p => p.name).ToArray();
            cfanJson.categories = modJson.tags.Select(p => p.name).ToArray();
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
