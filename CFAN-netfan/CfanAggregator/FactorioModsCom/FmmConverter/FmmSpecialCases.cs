using System.Collections.Generic;
using System.IO;
using CFAN_netfan.CfanAggregator.FactorioModsCom.Schema;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom.FmmConverter
{
    class FmmSpecialCases : IFmmConverter
    {
        protected ModDirectoryManager localManager;

        public FmmSpecialCases(ModDirectoryManager localManager)
        {
            this.localManager = localManager;
        }

        public IEnumerable<CfanJson> generateCfanJsons(IUser user, ModJson modJson)
        {
            if (modJson.name == "5dim´s mod")
            {
                CfanJson cfan = localManager.generateCfanFromModPackJsonFile(user, Path.Combine(localManager.RepoPacksDirectoryPath, "5Dim-5dim-0.0.0.json"), new Dictionary<string, string>()
                {
                    ["fmm-id"] = modJson.id.ToString()
                });
                return new CfanJson[] {cfan};
            }
            return null;
        }
    }
}
