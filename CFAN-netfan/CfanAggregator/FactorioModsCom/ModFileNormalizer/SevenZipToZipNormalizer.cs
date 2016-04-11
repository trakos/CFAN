using System;
using System.IO;
using System.Threading;
using CFAN_netfan.Compression;
using ICSharpCode.SharpZipLib.Zip;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom.ModFileNormalizer
{
    class SevenZipToZipNormalizer : IModFileNormalizer
    {
        public void normalizeModFile(string pathToWouldBeZip, string expectedRootDirectoryName)
        {
            if (!SimpleSevenZip.IsSevenZipFile(pathToWouldBeZip))
            {
                return;
            }
            string tempDirectory = getTemporaryDirectory();
            SimpleSevenZip.ExtractSevenZip(pathToWouldBeZip, tempDirectory);
            File.Delete(pathToWouldBeZip);
            new FastZip().CreateZip(pathToWouldBeZip, tempDirectory, recurse: true, fileFilter: null);
            Directory.Delete(tempDirectory, true);
        }

        private static string getTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
