using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CKAN;
using CKAN.Factorio;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;
using Newtonsoft.Json;

namespace CFAN_netfan.CfanAggregator.LocalRepository
{
    class LocalRepositoryManager
    {
        protected string repoUrlPrefix;
        protected string repoLocalPath;
        public string repoModsPath => Path.Combine(repoLocalPath, "mods");
        public string repoPacksPath => Path.Combine(repoLocalPath, "packs");
        public string repoModsUrl => repoUrlPrefix + "/mods/";

        public LocalRepositoryManager(string repoUrlPrefix, string repoLocalPath)
        {
            this.repoUrlPrefix = repoUrlPrefix;
            this.repoLocalPath = repoLocalPath;
        }

        public CfanJson generateCfanFromZipFile(IUser user, string file)
        {
            if (Path.GetExtension(file) != ".zip")
            {
                throw new Exception($"Unexpected file '{file}' in mods directory!");
            }
            CfanJson cfanJson = FactorioModParser.createCfanJsonFromFile(file);
            cfanJson.aggregatorData = new Dictionary<string, string>
            {
                ["x-source"] = typeof (LocalRepositoryAggregator).Name
            };
            cfanJson.downloadUrls = new string[] {repoModsUrl + Path.GetFileName(file)};
            return cfanJson;
        }

        public CfanJson generateCfanFromModPackJsonFile(IUser user, string file)
        {
            if (Path.GetExtension(file) != ".json")
            {
                throw new Exception($"Unexpected file '{file}' in packs directory!");
            }
            ModListJson modList = JsonConvert.DeserializeObject<ModListJson>(File.ReadAllText(file, Encoding.UTF8));
            string[] splitStrings = Path.GetFileNameWithoutExtension(file).Split(new[] { '-' }, 3);
            string author = splitStrings[0];
            string nameAndTitle = splitStrings[1];
            string version = splitStrings[2];
            return new CfanJson
            {
                modInfo = new ModInfoJson
                {
                    name = nameAndTitle,
                    title = nameAndTitle,
                    author = author,
                    version = new ModVersion(version),
                    description =
                        $"This a meta-package that will install all mods from the modpack {nameAndTitle} by {author}.",
                    dependencies =
                        modList.mods.Where(p => p.enabled == ModListJson.ModListJsonTruthy.YES)
                            .Select(p => new ModDependency(p.name))
                            .ToArray()
                },
                aggregatorData = new Dictionary<string, string> { ["x-source"] = typeof(LocalRepositoryAggregator).Name },
                authors = new[] { author },
                categories = new string[0],
                downloadSize = 0,
                downloadUrls = new string[0],
                type = CfanJson.CfanModType.META,
                suggests =
                    modList.mods.Where(p => p.enabled == ModListJson.ModListJsonTruthy.NO).Select(p => new ModDependency(p.name)).ToArray(),
                conflicts = new ModDependency[0],
                recommends = new ModDependency[0],
                tags = new string[0],
                releasedAt = null
            };
        }
    }
}
