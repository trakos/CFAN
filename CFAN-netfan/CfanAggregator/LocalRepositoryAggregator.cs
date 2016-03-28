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

namespace CFAN_netfan.CfanAggregator
{
    internal class LocalRepositoryAggregator : ICfanAggregator
    {
        protected string repoUrlPrefix;
        protected string repoLocalPath;
        protected string repoModsPath => Path.Combine(repoLocalPath, "mods");
        protected string repoPacksPath => Path.Combine(repoLocalPath, "packs");
        protected string repoModsUrl => repoLocalPath + "mods" + "/";

        public LocalRepositoryAggregator(string repoUrlPrefix, string repoLocalPath)
        {
            this.repoUrlPrefix = repoUrlPrefix;
            this.repoLocalPath = repoLocalPath;
        }


        public IList<CfanJson> getAllCfanJsons(IUser user)
        {
            List<CfanJson> cfanJsons = new List<CfanJson>();
            cfanJsons.AddRange(getAllModsCfanJsons(user));
            cfanJsons.AddRange(getAllMetaPacksCfanJsons(user));
            return cfanJsons;
        }

        public void mergeCfanJson(IUser user, CfanJson destination, CfanJson source)
        {
            throw new NotImplementedException();
        }

        protected IEnumerable<CfanJson> getAllModsCfanJsons(IUser user)
        {
            return Directory.EnumerateFiles(repoModsPath).Select(p => generateCfanFromZipFile(user, p));
        }

        protected CfanJson generateCfanFromZipFile(IUser user, string file)
        {
            if (Path.GetExtension(file) != ".zip")
            {
                throw new Exception($"Unexpected file '{file}' in mods directory!");
            }
            ModInfoJson modInfo = FactorioModParser.parseMod(file);
            if (modInfo == null)
            {
                throw new Exception($"Couldn't parse info.json from '{file}'!");
            }
            return new CfanJson
            {
                modInfo = modInfo,
                aggregatorData = new Dictionary<string, string> {["x-source"] = typeof (LocalRepositoryAggregator).Name},
                authors = modInfo.author.Split(',').Select(p => p.Trim()).ToArray(),
                categories = new string[0],
                downloadSize = new System.IO.FileInfo(file).Length,
                downloadUrls = new string[] {repoUrlPrefix + "/mods/" + Path.GetFileName(file)},
                releasedAt = null,
                suggests = new ModDependency[0],
                recommends = new ModDependency[0],
                conflicts = new ModDependency[0],
                tags = new string[0],
                type = CfanJson.CfanModType.MOD
            };
        }

        protected IEnumerable<CfanJson> getAllMetaPacksCfanJsons(IUser user)
        {
            return Directory.EnumerateFiles(repoPacksPath).Select(p => generateCfanFromModPackJsonFile(user, p));
        }

        protected CfanJson generateCfanFromModPackJsonFile(IUser user, string file)
        {
            if (Path.GetExtension(file) != ".json")
            {
                throw new Exception($"Unexpected file '{file}' in packs directory!");
            }
            ModListJson modList = JsonConvert.DeserializeObject<ModListJson>(File.ReadAllText(file, Encoding.UTF8));
            string[] splitStrings = Path.GetFileNameWithoutExtension(file).Split(new[] {'-'}, 3);
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
                authors = new[] {author},
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
