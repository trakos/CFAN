using System.Collections.Generic;
using System.IO;
using CFAN_netfan.CfanAggregator.FactorioModsCom.Schema;
using CFAN_netfan.CfanAggregator.LocalRepository;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom.FmmConverter
{
    class FmmSpecialCases : IFmmConverter
    {
        protected LocalRepositoryManager localManager;

        public FmmSpecialCases(LocalRepositoryManager localManager)
        {
            this.localManager = localManager;
        }

        public IEnumerable<CfanJson> generateCfanJsons(IUser user, ModJson modJson)
        {
            if (modJson.name == "5dim´s mod")
            {
                CfanJson cfan = localManager.generateCfanFromModPackJsonFile(user, Path.Combine(localManager.repoPacksPath, "5Dim-5dim-0.0.0.json"));
                cfan.aggregatorData["fmm-id"] = modJson.id.ToString();
                return new CfanJson[] {cfan};
            }
            return null;
        }
    }
}
