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
            CfanJson cfanJson = CfanGenerator.createCfanJsonFromFile(file);
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
            string[] splitStrings = Path.GetFileNameWithoutExtension(file).Split(new[] { '-' }, 3);
            string author = splitStrings[0];
            string nameAndTitle = splitStrings[1];
            ModVersion version = new ModVersion(splitStrings[2]);
            string description = $"This is a meta-package that will install all mods from the modpack {nameAndTitle} by {author}.";
            return CfanGenerator.createCfanJsonFromModListJson(file, nameAndTitle, nameAndTitle, version, author, description);
        }
    }
}
