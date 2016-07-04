using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CKAN.Factorio
{
    static public class CfanGenerator
    {
        public static CfanJson createCfanJsonFromModListJson(string file, string name, string title, ModVersion version, string author, string description = "")
        {
            ModListJson modList = JsonConvert.DeserializeObject<ModListJson>(File.ReadAllText(file, Encoding.UTF8));
            CfanJson cfanJson = createEmptyMetaCfanJson(name, title, version, author, description);
            cfanJson.modInfo.dependencies = modList.mods.Where(p => p.enabled == ModListJson.ModListJsonTruthy.YES)
                            .Select(p => new ModDependency(p.name))
                            .ToArray();
            cfanJson.suggests = modList.mods.Where(p => p.enabled == ModListJson.ModListJsonTruthy.NO).Select(p => new ModDependency(p.name)).ToArray();
            return cfanJson;
        }

        public static CfanJson createEmptyMetaCfanJson(string name, string title, ModVersion version, string author = "", string description = "")
        {
            return new CfanJson
            {
                modInfo = new ModInfoJson
                {
                    name = name,
                    title = title,
                    author = author,
                    version = version,
                    description = description,
                    dependencies = new ModDependency[0]
                },
                aggregatorData = new Dictionary<string, string>(),
                authors = new string[] { author },
                categories = new string[0],
                downloadSize = 0,
                downloadUrls = new string[0],
                type = CfanJson.CfanModType.META,
                suggests = new ModDependency[0],
                conflicts = new ModDependency[0],
                recommends = new ModDependency[0],
                tags = new string[0],
                releasedAt = null
            };
        }

        public static CfanJson createCfanJsonFromFile(string directoryOrZipFile)
        {
            ModInfoJson modInfo = FactorioModParser.parseMod(directoryOrZipFile);
            if (modInfo == null)
            {
                throw new Exception($"Couldn't parse info.json from '{directoryOrZipFile}'!");
            }
            return createCfanJsonFromModInfoJson(modInfo, new System.IO.FileInfo(directoryOrZipFile).Length);
        }

        public static CfanJson createCfanJsonFromModInfoJson(ModInfoJson modInfo, long downloadSize)
        {
            return new CfanJson
            {
                modInfo = modInfo,
                aggregatorData = new Dictionary<string, string>(),
                authors = modInfo.author.Split(',').Select(p => p.Trim()).ToArray(),
                categories = new string[0],
                downloadSize = downloadSize,
                downloadUrls = new string[0],
                releasedAt = null,
                suggests = new ModDependency[0],
                recommends = new ModDependency[0],
                conflicts = new ModDependency[0],
                tags = new string[0],
                type = CfanJson.CfanModType.MOD
            };

        }
    }
}
