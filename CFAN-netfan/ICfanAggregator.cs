using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan
{
    public interface ICfanAggregator
    {
        IList<CfanJson> getAllCfanJsons(IUser user);
        void mergeCfanJson(IUser user, CfanJson destination, CfanJson source);
    }
}
