using System;
using NUnrar.Archive;
using NUnrar.Common;

namespace CFAN_netfan.Compression
{
    internal static class SimpleRar
    {
        public static void ExtractRar(string rarFileName, string targetDirectory)
        {
            { 
                RarArchive.WriteToDirectory(rarFileName, targetDirectory, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
            }
            // there is some poor coding in RarArchive apparently that causes rarFileName to still be used after extracting - GC.Collect handles that 
            GC.Collect();
        }

        public static bool IsRarFile(string rarFileName)
        {
            return RarArchive.IsRarFile(rarFileName);
        }
    }
}
