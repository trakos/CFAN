using System;
using CKAN.Factorio;
using CKAN.Factorio.Version;

namespace CKAN
{
    /// <summary>
    /// You're On Your Own (YOYO) game compatibility comparison.
    /// This claims everything is compatible with everything.
    /// </summary>
    public class YoyoGameComparator : IGameComparator
    {
        public bool Compatible(FactorioVersion gameVersion, CfanModule module)
        {
            return true;
        }
    }
}