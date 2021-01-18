using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;

namespace CFAN_netfan.Compression
{
    internal class SimpleSevenZip
    {

        public static void ExtractSevenZip(string zipFileName, string targetDirectory)
        {
            ArchiveFactory.WriteToDirectory(zipFileName, targetDirectory);
        }

        public static bool IsSevenZipFile(string sevenZipFilename)
        {
            return SevenZipArchive.IsSevenZipFile(sevenZipFilename);
        }
    }
}
