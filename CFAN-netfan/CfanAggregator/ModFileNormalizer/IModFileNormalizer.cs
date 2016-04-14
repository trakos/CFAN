namespace CFAN_netfan.CfanAggregator.ModFileNormalizer
{
    interface IModFileNormalizer
    {
        void normalizeModFile(string path, string expectedRootDirectoryName);
    }
}
