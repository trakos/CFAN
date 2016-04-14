using System.Collections.Generic;
using System.Linq;

namespace CFAN_netfan.CfanAggregator.ModFileNormalizer
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
