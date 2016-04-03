namespace CFAN_netfan.CfanAggregator.FactorioModsCom.ModFileNormalizer
{
    interface IModFileNormalizer
    {
        void normalizeModFile(string path, string expectedRootDirectoryName);
    }
}
