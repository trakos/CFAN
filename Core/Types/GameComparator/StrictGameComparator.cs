using System;
using CKAN.Factorio;
using CKAN.Factorio.Version;

namespace CKAN
{
    /// <summary>
    /// Test to see if a module is compatible with the user's installed game,
    /// using strict tests.
    /// </summary>
    public class StrictGameComparator : IGameComparator
    {

        public bool Compatible(FactorioVersion gameVersion, CfanModule module)
        {
            return module.IsCompatibleKSP(gameVersion);
        }
    }
}

