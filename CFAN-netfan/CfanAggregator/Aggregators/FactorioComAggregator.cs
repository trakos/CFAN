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
using System.Threading;

namespace CFAN_netfan.CfanAggregator.Aggregators
{
    public class FactorioComAggregator : ICfanAggregator
    {
        public const string BASE_URI = "https://mods.factorio.com";
        const string FIRST_PAGE = "/api/mods";

        protected FactorioComDownloader downloader;
        protected List<ModsPageJson> prefetchedModsPages;

        public FactorioComAggregator()
        {
            this.downloader = new FactorioComDownloader();
            prefetchedModsPages = fetchAllModsInfo();
        }

        public List<ModsPageJson> fetchAllModsInfo()
        {
            List<ModsPageJson> modsPages = new List<ModsPageJson>();
            string nextPageUrl = BASE_URI + FIRST_PAGE;
            for (int pageNumber = 1; pageNumber <= 1000; pageNumber++)
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
                        throw new Kraken($"Couldn't fetch page {pageNumber} of mods list from mods.factorio.com", e);
                    }
                    var modsPage = JsonConvert.DeserializeObject<ModsPageJson>(jsonString);
                    modsPages.Add(modsPage);

                    if (string.IsNullOrEmpty(modsPage.pagination.links.next))
                    {
                        return modsPages;
                    }
                    nextPageUrl = modsPage.pagination.links.next;
                    Thread.Sleep(100);
                }
            }

            throw new Exception("Expected less than 1000 pages.");
        }

        public IEnumerable<CfanJson> getAllCfanJsons(IUser user)
        {
            List<CfanJson> cfans = new List<CfanJson>();
            foreach (var modsPage in prefetchedModsPages)
            {
                cfans.AddRange(modsPage.results.SelectMany(p => downloader.generateCfanJsons(user, p)));
            }
            return cfans;
        }

        public void mergeCfanJson(IUser user, CfanJson destination, CfanJson source)
        {
            // ignore, this requires auth that almost no one yet has in config (0.13 is in unstable atm)
            // so avoid it if possible
        }
    }
}
