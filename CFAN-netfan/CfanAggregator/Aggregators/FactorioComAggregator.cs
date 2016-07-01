using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CFAN_netfan.CfanAggregator.FactorioCom;
using CFAN_netfan.CfanAggregator.FactorioCom.Schema;
using CKAN;
using CKAN.Factorio.Schema;
using Newtonsoft.Json;

namespace CFAN_netfan.CfanAggregator.Aggregators
{
    class FactorioComAggregator : ICfanAggregator
    {
        public const string BASE_URI = "https://mods.factorio.com";
        const string FIRST_PAGE = "/api/mods";

        protected FactorioComDownloader downloader;

        public FactorioComAggregator()
        {
            this.downloader = new FactorioComDownloader();
        }

        public IEnumerable<CfanJson> getAllCfanJsons(IUser user)
        {
            List<CfanJson> cfans = new List<CfanJson>();
            string nextPageUrl = BASE_URI + FIRST_PAGE;
            for (int pageNumber = 1; pageNumber <= 30; pageNumber++)
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    string jsonString;
                    try
                    {
                        jsonString = wc.DownloadString(nextPageUrl);
                    }
                    catch (Exception e)
                    {
                        throw new Kraken("Couldn't fetch mods list from mods.factorio.com", e);
                    }
                    var modsPage = JsonConvert.DeserializeObject<ModsPageJson>(jsonString);
                    cfans.AddRange(modsPage.results.SelectMany(p => downloader.generateCfanJsons(user, p)));

                    if (string.IsNullOrEmpty(modsPage.pagination.links.next))
                    {
                        return cfans;
                    }
                    nextPageUrl = modsPage.pagination.links.next;
                }
            }

            throw new Exception("Expected less than 30 pages.");
        }

        public void mergeCfanJson(IUser user, CfanJson destination, CfanJson source)
        {
            // ignore, this requires auth that almost no one yet has in config (0.13 is in unstable atm)
            // so avoid it if possible
        }
    }
}
