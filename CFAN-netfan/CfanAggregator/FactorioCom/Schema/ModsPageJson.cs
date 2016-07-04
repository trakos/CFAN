using Newtonsoft.Json;

namespace CFAN_netfan.CfanAggregator.FactorioCom.Schema
{
    class ModsPageJson
    {
        [JsonProperty(Required = Required.Always)]
        public PaginationJson pagination;
        [JsonProperty(Required = Required.Always)]
        public ModJson[] results;

        public class PaginationJson
        {
            public uint count;
            public uint page_size;
            public LinksJson links;
            public uint page_count;
            public uint page;

            public class LinksJson
            {
                public string next;
                public string first;
                public string last;
                public string prev;
            }
        }
    }
}
