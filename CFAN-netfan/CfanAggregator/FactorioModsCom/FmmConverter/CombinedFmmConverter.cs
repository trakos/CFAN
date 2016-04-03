using System;
using System.Collections.Generic;
using CFAN_netfan.CfanAggregator.FactorioModsCom.Schema;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom.FmmConverter
{
    class CombinedFmmConverter : IFmmConverter
    {
        protected IEnumerable<IFmmConverter> fmmConverters;

        public CombinedFmmConverter(IEnumerable<IFmmConverter> fmmConverters)
        {
            this.fmmConverters = fmmConverters;
        }


        public IEnumerable<CfanJson> generateCfanJsons(IUser user, ModJson modJson)
        {
            foreach (IFmmConverter fmmConverter in fmmConverters)
            {
                IEnumerable<CfanJson> result = fmmConverter.generateCfanJsons(user, modJson);
                if (result != null)
                {
                    return result;
                }
            }
            throw new Exception($"None of the converters wanted to convert mod {modJson.name}.");
        }
    }
}
