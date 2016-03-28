using System.IO;
using System.Linq;

namespace CKAN.Exporters
{
    public sealed class BbCodeExporter : IExporter
    {
        public void Export(IRegistryQuerier registry, Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("[LIST]");

                foreach (var mod in registry.InstalledModules.OrderBy(i => i.Module.identifier))
                {
                    writer.WriteLine(@"[*][B]{0}[/B] ({1} {2})", mod.Module.title, mod.identifier, mod.Module.modVersion);
                }

                writer.WriteLine("[/LIST]");
            }
        }
    }
}
