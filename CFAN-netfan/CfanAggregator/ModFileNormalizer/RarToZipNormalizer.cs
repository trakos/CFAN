using System;
using System.IO;
using System.Threading;
using CFAN_netfan.Compression;
using ICSharpCode.SharpZipLib.Zip;

namespace CFAN_netfan.CfanAggregator.ModFileNormalizer
{
    class RarToZipNormalizer : IModFileNormalizer
    {
        public void normalizeModFile(string pathToWouldBeZip, string expectedRootDirectoryName)
        {
            if (!SimpleRar.IsRarFile(pathToWouldBeZip))
            {
                return;
            }
            string tempDirectory = getTemporaryDirectory();
            SimpleRar.ExtractRar(pathToWouldBeZip, tempDirectory);
            try
            {
                File.Delete(pathToWouldBeZip);
            }
            catch (Exception)
            {
                Thread.Sleep(500);
                GC.Collect();
                File.Delete(pathToWouldBeZip);
            }
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
