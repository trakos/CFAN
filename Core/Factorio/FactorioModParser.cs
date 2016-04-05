using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using ICSharpCode.SharpZipLib.Zip;
using log4net;
using Newtonsoft.Json;

namespace CKAN.Factorio
{
    public class FactorioModParser
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(KSP));

        public static ModInfoJson parseMod(string directoryOrZipFile)
        {
            string json = getInfoJsonTextContent(directoryOrZipFile);
            return JsonConvert.DeserializeObject<ModInfoJson>(json);
        }

        protected static string getInfoJsonTextContent(string directoryOrZipFile)
        {
            if (Directory.Exists(directoryOrZipFile))
            {
                string infoJsonPath = Path.Combine(directoryOrZipFile, "info.json");
                if (!File.Exists(infoJsonPath))
                {
                    throw new ArgumentException($"Couldn't detect mod in directory {directoryOrZipFile}", nameof(directoryOrZipFile));
                }
                try
                {
                    return File.ReadAllText(infoJsonPath);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"Couldn't read {infoJsonPath}: {e.Message}", nameof(directoryOrZipFile), e);
                }
            }

            if (File.Exists(directoryOrZipFile))
            {
                try
                {
                    return getInfoJsonTextFromZip(directoryOrZipFile);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"Couldn't read info.json from {directoryOrZipFile}: {e.Message}", nameof(directoryOrZipFile), e);
                }
            }

            throw new ArgumentException($"File {directoryOrZipFile} is not a zipped mod or a directory containing a mod", nameof(directoryOrZipFile));
        }

        protected static string getInfoJsonTextFromZip(string pathToZip)
        {
            using (ZipFile zipFile = new ZipFile(File.OpenRead(pathToZip)))
            {
                string rootDirectoryName = getRootDirectoryName(zipFile);
                ZipEntry entry = zipFile.GetEntry(rootDirectoryName + "/info.json");
                if (entry == null)
                {
                    throw new InvalidEntryInModsDirectoryKraken($"Zip file {pathToZip} does not contain info.json");
                }
                if (!entry.IsFile)
                {
                    throw new InvalidEntryInModsDirectoryKraken($"Mod {pathToZip} contains info.json that is not a file");
                }
                var stream = zipFile.GetInputStream(entry);
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        // @todo: that library is bad or I REALLY don't know how to use it
        protected static string getRootDirectoryName(ZipFile zipFile)
        {
            return zipFile
                .Cast<ZipEntry>()
                .First()
                .Name.Split(new[] {'/', '\\'}, 2, StringSplitOptions.RemoveEmptyEntries)[0];
        }
    }

}
