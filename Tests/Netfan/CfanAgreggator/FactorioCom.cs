using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CFAN_netfan.CfanAggregator.Aggregators;
using CFAN_netfan.CfanAggregator.FactorioCom;
using Newtonsoft.Json;
using CFAN_netfan.CfanAggregator.FactorioCom.Schema;
using CKAN;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;

namespace Tests.Netfan.CfanAgreggator
{
    [TestFixture]
    class FactorioCom
    {
        [Test]
        public void NormalizeFactorioVersion()
        {
            var jsonString = @"{""tags"":[{""id"":21,""name"":""storage"",""title"":""Storage"",""description"":"""",""type"":""t""}],""ratings_count"":0,""game_versions"":[""0.13""],""license_url"":""https://opensource.org/licenses/MIT"",""latest_release"":{""id"":11169,""version"":""0.1.0"",""game_version"":""0.13"",""released_at"":""2017-12-18T08:51:02.518735Z"",""download_url"":""/api/downloads/data/mods/168/Warehousing_0.1.0.zip"",""info_json"":{""description"":""Store all the things! Warehousing provides high capacity storage buildings, including logistic network versions."",""author"":""Anoyomouse"",""name"":""Warehousing"",""dependencies"":[""base >= 0.15.0""],""contact"":""PM on the Factorio Forums"",""version"":""0.1.0"",""factorio_version"":""0.16"",""homepage"":""https://forums.factorio.com/17295"",""title"":""Warehousing Mod""},""file_name"":""Warehousing_0.1.0.zip"",""file_size"":609499,""downloads_count"":2381,""factorio_version"":""0.16""},""summary"":""Store all the things! Warehousing provides high capacity storage buildings, including logistic network versions."",""id"":168,""license_name"":""MIT"",""created_at"":""2016-06-30 14:31:27.945831+00:00"",""name"":""Warehousing"",""github_path"":""Anoyomouse/Warehousing"",""updated_at"":""2017-12-18 08:51:02.892619+00:00"",""first_media_file"":{""id"":302,""width"":686,""height"":489,""size"":816769,""urls"":{""original"":""https://mods-data.factorio.com/pub_data/media_files/jEL61ah7Nqxg.png"",""thumb"":""https://mods-data.factorio.com/pub_data/media_files/jEL61ah7Nqxg.thumb.png""}},""license_flags"":599,""title"":""Warehousing Mod"",""current_user_rating"":null,""downloads_count"":88876,""owner"":""anoyomouse"",""homepage"":""https://forums.factorio.com/17295""}";
            var modJson = JsonConvert.DeserializeObject<ModJson>(jsonString);
            FactorioComDownloader downloader = new FactorioComDownloader();
            IEnumerable<CfanJson> mods = downloader.generateCfanJsons(new NullUser(), modJson);
            CfanJson cfanJson = mods.First();
            ModDependency factorioDependency = cfanJson.modInfo.dependencies.First(p => p.modName == "base");
            Assert.IsTrue(factorioDependency.maxVersion > new ModVersion("0.16.0"));
        }
    }
}
