using System;
using CKAN.Factorio;
using CKAN.Factorio.Version;

namespace CKAN
{
    /// <summary>
    /// Test to see if the user's game is "Generally Recognised As Safe" (GRAS) with a given mod,
    /// with extra understanding of which KSP versions are "safe" (ie: 1.0.5 mostly works with 1.0.4 mods).
    /// If the mod has `ksp_version_strict` set then this is identical to strict checking.
    /// </summary>
    public class GrasGameComparator : IGameComparator
    {
        static readonly StrictGameComparator strict = new StrictGameComparator();

        public bool Compatible(FactorioVersion gameVersion, CfanModule module)
        {
            // If it's strictly compatible, then it's compatible.
            if (strict.Compatible(gameVersion, module))
                return true;

            // if we will add something like "maximum game version" for a mod, then it might make sense to add something here (see summary of the class)

            return false;
        }
    }
}

