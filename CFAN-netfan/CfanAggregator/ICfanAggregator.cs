﻿using System.Collections.Generic;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator
{
    public interface ICfanAggregator
    {
        IEnumerable<CfanJson> getAllCfanJsons(IUser user);
        void mergeCfanJson(IUser user, CfanJson destination, CfanJson source);
    }
}
