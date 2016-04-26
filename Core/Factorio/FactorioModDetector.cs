using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;
using ICSharpCode.SharpZipLib.Zip;
using log4net;
using Newtonsoft.Json;

namespace CKAN.Factorio
{
    class FactorioModDetector
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(KSP));

        public static IDictionary<string, ModInfoJson> findAllModsInDirectory(string directory)
        {
            IDictionary<string, ModInfoJson> modInfosList = new Dictionary<string, ModInfoJson>();
            ModInfoJson modInfo;
            foreach (var modDirectory in Directory.GetDirectories(directory))
            {
                modInfo = parsePotentialMod(modDirectory);
                if (modInfo != null)
                {
                    modInfosList[KSPPathUtils.NormalizePath(modDirectory)] = modInfo;
                }
            }
            foreach (var modFile in Directory.GetFiles(directory).Where(p => Path.GetFileName(p) != "mod-list.json"))
            {
                modInfo = parsePotentialMod(modFile);
                if (modInfo != null)
                {
                    modInfosList[KSPPathUtils.NormalizePath(modFile)] = modInfo;
                }
            }
            return modInfosList;
        }

        protected static ModInfoJson parsePotentialMod(string directoryOrZipFile)
        {
            try
            {
                return FactorioModParser.parseMod(directoryOrZipFile);
            }
            catch (Exception e)
            {
                log.WarnFormat("Couldn't parse potential mod in {0}: {1}", directoryOrZipFile, e.Message);
                log.Debug(e, e);
                return null;
            }
        }

        public static ModVersion getModVersion(string path)
        {
            return FactorioModParser.parseMod(path).version;
        }
    }
}
