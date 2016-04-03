using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CFAN_netfan.Compression;
using ICSharpCode.SharpZipLib.Zip;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom.ModFileNormalizer
{
    class ModZipRootNormalizer : IModFileNormalizer
    {
        public void normalizeModFile(string pathToZip, string expectedDirectoryName)
        {
            string foundRootDirectory;
            if (!shouldZipBeNormalized(pathToZip, expectedDirectoryName, out foundRootDirectory))
            {
                return;
            }
            // prepare
            string extractZipDir = getTemporaryDirectory();
            string createZipDir = getTemporaryDirectory();

            // extract zip, delete zip, move root, create new zip with fixed root
            SimpleZip.ExtractZip(pathToZip, extractZipDir);
            Directory.Move(Path.Combine(extractZipDir, foundRootDirectory), Path.Combine(createZipDir, expectedDirectoryName));
            File.Delete(pathToZip);
            SimpleZip.CreateZip(pathToZip, createZipDir);

            // cleanup - if found root directory was empty, we moved the entire directory and there's nothing to delete
            if (foundRootDirectory != "")
            {
                Directory.Delete(extractZipDir, true);
            }
            Directory.Delete(createZipDir, true);
        }

        private static string getTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private static bool shouldZipBeNormalized(string pathToZip, string expectedDirectoryName, out string foundRootDirectoryName)
        {
            using (ZipFile zipFile = new ZipFile(File.OpenRead(pathToZip)))
            {
                ZipEntry entry = getInfoJsonEntry(zipFile, $"zip: ${pathToZip} mod: ${expectedDirectoryName}");
                foundRootDirectoryName = getParentDirectory(entry.Name);
                return foundRootDirectoryName != expectedDirectoryName;
            }
        }

        private static ZipEntry getInfoJsonEntry(ZipFile zipFile, string exceptionHelperText)
        {
            ZipEntry[] infoJsonEntries = zipFile
                .Cast<ZipEntry>()
                .Where(p => p.Name.EndsWith("/info.json") || p.Name.Equals("info.json"))
                .ToArray();
            if (!infoJsonEntries.Any())
            {
                throw new Exception($"Zip file does not have info.json inside (${exceptionHelperText})");
            }
            if (infoJsonEntries.Length == 1)
            {
                return infoJsonEntries[0];
            }
            // now if there's more than one we will return only if there is info.json that is above all other info.jsons in directory structure
            int shortestEntryNameLength = infoJsonEntries.Min(p => p.Name.Length);
            ZipEntry[] shortestInfoJsonEntries = infoJsonEntries.Where(p => p.Name.Length == shortestEntryNameLength).ToArray();
            // same length for more than one info.json means there can't be info.json that would be above other
            if (shortestInfoJsonEntries.Length > 1)
            {
                throw new Exception($"Zip file has more than one info.json with same path length (${exceptionHelperText})");
            }
            // if all names starts with parent dir of first info.json we're all good
            // there is a special case where parentDirectory is just ""
            ZipEntry shortestInfoJsonEntry = shortestInfoJsonEntries.First();
            string parentDirectory = getParentDirectory(shortestInfoJsonEntry.Name);
            if (infoJsonEntries.All(p => p.Name.StartsWith(parentDirectory)))
            {
                return shortestInfoJsonEntry;
            }
            throw new Exception($"Zip file has more than one info.json in different directories (${exceptionHelperText})");
        }

        // we have to use this method because Path. uses windows directory separators
        private static string getParentDirectory(string zipPath)
        {
            return string.Join("/", splitZipPath(zipPath).Reverse().Skip(1).Reverse());
        }

        // we have to use this method because Path. uses windows directory separators
        private static string[] splitZipPath(string zipPath)
        {
            return zipPath.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
