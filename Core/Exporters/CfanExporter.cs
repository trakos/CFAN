using CKAN.Factorio;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace CKAN.Exporters
{
    public class CfanExporter : IExporter
    {
        protected bool withVersions;

        public CfanExporter(bool withVersions)
        {
            this.withVersions = withVersions;
        }

        public void Export(IRegistryQuerier registry, Stream stream)
        {
            string description = "Saved CFAN mods from " + DateTime.Now.ToLongDateString();
            CfanJson metaCfan = CfanGenerator.createEmptyMetaCfanJson("saved-cfan-mods", "Saved CFAN mods", new ModVersion("0.0.0"), "CFAN user", description);
            metaCfan.modInfo.dependencies = registry.Installed(false).Select(p => new ModDependency(withVersions ? p.Key + " == " + p.Value : p.Key)).ToArray();
            string cfanJsonString = JsonConvert.SerializeObject(metaCfan);

            using (var writer = new StreamWriter(stream))
            {
                writer.Write(cfanJsonString);
            }
        }
    }
}
