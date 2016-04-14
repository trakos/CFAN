using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using CFAN_netfan.CfanAggregator.FactorioModsCom.FmmConverter;
using CFAN_netfan.CfanAggregator.FactorioModsCom.Schema;
using CKAN;
using CKAN.Factorio.Schema;
using Newtonsoft.Json;

namespace CFAN_netfan.CfanAggregator.Aggregators
{
    class FactorioModsComAggregator : ICfanAggregator
    {
        const string BASE_URI = "http://api.factoriomods.com/mods?page=";
        
        protected IFmmConverter fmmConverter;

        public FactorioModsComAggregator(ModDirectoryManager localManager, ModDirectoryManager fmmManager)
        {
            this.fmmConverter = new CombinedFmmConverter(new List<IFmmConverter>()
            {
                new FmmIgnorer(),
                new FmmSpecialCases(localManager),
                new FmmDownloader(fmmManager)
            });
        }

        public IEnumerable<CfanJson> getAllCfanJsons(IUser user)
        {
            List<CfanJson> cfans = new List<CfanJson>();
            for (int pageNumber = 1; pageNumber <= 15; pageNumber++)
            {
                string uri = BASE_URI + pageNumber.ToString();
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    string jsonString = wc.DownloadString(uri);
                    var mods = JsonConvert.DeserializeObject<ModJson[]>(jsonString);
                    if (!mods.Any())
                    {
                        return cfans;
                    }
                    cfans.AddRange(mods.SelectMany(p => fmmConverter.generateCfanJsons(user, p)));
                }
            }

            throw new Exception("Expected less than 15 pages.");
        }

        public void mergeCfanJson(IUser user, CfanJson destination, CfanJson source)
        {
            destination.aggregatorData["fmm-id"] = source.aggregatorData["fmm-id"];
        }

    }
}
