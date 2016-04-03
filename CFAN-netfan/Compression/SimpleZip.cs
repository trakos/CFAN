using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace CFAN_netfan.Compression
{
    class SimpleZip
    {
        // FastZip messes paths on mono
        public static void ExtractZip(string zipFileName, string targetDirectory)
        {
            using (var zipFile = new ZipFile(zipFileName))
            {
                foreach (var zipEntry in zipFile.OfType<ZipEntry>().Where(p => p.IsFile))
                {
                    var unzipPath = Path.Combine(targetDirectory, zipEntry.Name);
                    var directoryPath = Path.GetDirectoryName(unzipPath);

                    // create directory if needed
                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // unzip the file
                    var zipStream = zipFile.GetInputStream(zipEntry);
                    var buffer = new byte[4096];

                    using (var unzippedFileStream = File.Create(unzipPath))
                    {
                        StreamUtils.Copy(zipStream, unzippedFileStream, buffer);
                    }
                }
            }
        }

        public static void CreateZip(string zipFileName, string targetDirectory)
        {
            new FastZip().CreateZip(zipFileName, targetDirectory, recurse: true, fileFilter: null);
        }
    }
}
