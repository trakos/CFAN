using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CFAN_netfan.CfanAggregator.LocalRepository;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator
{
    internal class LocalRepositoryAggregator : ICfanAggregator
    {
        protected LocalRepositoryManager localManager;

        public LocalRepositoryAggregator(LocalRepositoryManager localManager)
        {
            this.localManager = localManager;
        }

        public virtual IList<CfanJson> getAllCfanJsons(IUser user)
        {
            List<CfanJson> cfanJsons = new List<CfanJson>();
            cfanJsons.AddRange(getAllModsCfanJsons(user));
            cfanJsons.AddRange(getAllMetaPacksCfanJsons(user));
            return cfanJsons;
        }

        protected IEnumerable<CfanJson> getAllMetaPacksCfanJsons(IUser user)
        {
            return
                Directory.EnumerateFiles(this.localManager.repoPacksPath)
                    .Select(p => this.localManager.generateCfanFromModPackJsonFile(user, p));
        }

        protected IEnumerable<CfanJson> getAllModsCfanJsons(IUser user)
        {
            return
                Directory.EnumerateFiles(this.localManager.repoModsPath)
                    .Select(p => this.localManager.generateCfanFromZipFile(user, p));
        }

        public virtual void mergeCfanJson(IUser user, CfanJson destination, CfanJson source)
        {
            throw new NotImplementedException();
        }
    }
}
