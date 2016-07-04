using System.Collections.Generic;
using System.Linq;
using CFAN_netfan.CfanAggregator.FactorioModsCom.Schema;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom.FmmConverter
{
    class FmmIgnorer : IFmmConverter
    {
        public string[] ignoreNames =
        {
            // not a mod with info.json
            "canInsert", // single lua file
            "mapconverter", // exe tool
            "BrightenUp", // zip with images
            // other
            "dytech", // two mods and two cfan.jsons crammed into one zip file - maybe should handle it and split them
            "ScienceCostTweaker",
            "marathon",
            "More_Power_Factorio", "Power lines will now spawn between electricity using items", // scam
            "sleaf-Tower-Defense", // scenario with zip in rar
            "WaiTex" // handled by github
        };

        public IEnumerable<CfanJson> generateCfanJsons(IUser user, ModJson modJson)
        {
            return ignoreNames.Contains(modJson.name) ? new CfanJson[0] : null;
        }
    }
}
