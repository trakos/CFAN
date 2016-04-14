using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator.Aggregators
{
    internal class LocalRepositoryAggregator : ICfanAggregator
    {
        protected ModDirectoryManager localManager;

        public LocalRepositoryAggregator(ModDirectoryManager localManager)
        {
            this.localManager = localManager;
        }

        public virtual IEnumerable<CfanJson> getAllCfanJsons(IUser user)
        {
            List<CfanJson> cfanJsons = new List<CfanJson>();
            cfanJsons.AddRange(getAllModsCfanJsons(user));
            cfanJsons.AddRange(getAllMetaPacksCfanJsons(user));
            return cfanJsons;
        }

        protected IEnumerable<CfanJson> getAllMetaPacksCfanJsons(IUser user)
        {
            return
                Directory.EnumerateFiles(this.localManager.RepoPacksDirectoryPath)
                    .Select(p => this.localManager.generateCfanFromModPackJsonFile(user, p, new Dictionary<string, string>()));
        }

        protected IEnumerable<CfanJson> getAllModsCfanJsons(IUser user)
        {
            return
                Directory.EnumerateFiles(this.localManager.RepoModsDirectoryPath)
                    .Select(p => this.localManager.generateCfanFromZipFile(user, p, new Dictionary<string, string>
                    {
                        ["x-source"] = typeof (LocalRepositoryAggregator).Name
                    }));
        }

        public virtual void mergeCfanJson(IUser user, CfanJson destination, CfanJson source)
        {
            throw new NotImplementedException();
        }
    }
}
