namespace CFAN_netfan.CfanAggregator.FactorioCom.Schema
{
    class ModMediaFileJson
    {
        public uint id;
        public uint width;
        public uint height;
        public uint size;
        public UrlsJson urls;

        public class UrlsJson
        {
            string original;
            string thumb;
        }
    }
}
