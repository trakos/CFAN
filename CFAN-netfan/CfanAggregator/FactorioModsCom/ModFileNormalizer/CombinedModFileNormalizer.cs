using System;
using System.Collections.Generic;
using System.Linq;
using CFAN_netfan.CfanAggregator.FactorioModsCom.FmmConverter;
using CFAN_netfan.CfanAggregator.FactorioModsCom.Schema;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom.ModFileNormalizer
{
    class CombinedModFileNormalizer : IModFileNormalizer
    {
        protected List<IModFileNormalizer> modFileNormalizers;

        public CombinedModFileNormalizer(IEnumerable<IModFileNormalizer> modFileNormalizers)
        {
            this.modFileNormalizers = modFileNormalizers.ToList();
        }

        public void normalizeModFile(string path, string expectedRootDirectoryName)
        {
            modFileNormalizers.ForEach(p => p.normalizeModFile(path, expectedRootDirectoryName));
        }
    }
}
