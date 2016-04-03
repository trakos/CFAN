using System;
using System.Collections.Generic;
using System.Linq;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator
{
    public class CombinedCfanAggregator : ICfanAggregator
    {
        protected IEnumerable<ICfanAggregator> cfanAggregators;

        public CombinedCfanAggregator(IEnumerable<ICfanAggregator> cfanAggregators)
        {
            this.cfanAggregators = cfanAggregators;
        }

        public IList<CfanJson> getAllCfanJsons(IUser user)
        {
            List<CfanJson> result = new List<CfanJson>();
            foreach (ICfanAggregator cfanAggregator in cfanAggregators)
            {
                foreach (CfanJson newCfan in cfanAggregator.getAllCfanJsons(user))
                {
                    CfanJson existingCfan = result.FirstOrDefault(p => p.modInfo.name == newCfan.modInfo.name && p.modInfo.version == newCfan.modInfo.version);
                    if (existingCfan != null)
                    {
                        cfanAggregator.mergeCfanJson(user, existingCfan, newCfan);
                    }
                    else
                    {
                        result.Add(newCfan);
                    }
                }
            }
            return result;
        }

        public void mergeCfanJson(IUser user, CfanJson destination, CfanJson source)
        {
            throw new NotImplementedException();
        }
    }
}
