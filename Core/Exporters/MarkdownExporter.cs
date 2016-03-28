using System.IO;
using System.Linq;

namespace CKAN.Exporters
{
    public sealed class MarkdownExporter : IExporter
    {
        public void Export(IRegistryQuerier registry, Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                foreach (var mod in registry.InstalledModules.OrderBy(i => i.Module.identifier))
                {
                    writer.WriteLine(@"- **{0}** `{1} {2}`", mod.Module.title, mod.identifier, mod.Module.modVersion);
                }
            }
        }
    }
}
