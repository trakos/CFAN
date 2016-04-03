using System.Collections.Generic;
using CFAN_netfan.CfanAggregator.FactorioModsCom.Schema;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom.FmmConverter
{
    interface IFmmConverter
    {
        // returning null means try other converter, returning non-null means use that
        IEnumerable<CfanJson> generateCfanJsons(IUser user, ModJson modJson);
    }
}
