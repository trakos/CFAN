using System;
using System.IO;
using System.Linq;

namespace CKAN.Exporters
{
    public sealed class DelimeterSeperatedValueExporter : IExporter
    {
        private const string WritePattern = "{1}{0}{2}{0}{3}{0}{4}{0}{5}" +
                                            "{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}";
        private readonly string _delimter;

        public DelimeterSeperatedValueExporter(Delimter delimter)
        {
            switch (delimter)
            {
                case Delimter.Comma:
                    _delimter = ",";
                    break;
                case Delimter.Tab:
                    _delimter = "\t";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Export(IRegistryQuerier registry, Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(WritePattern,
                    _delimter,
                    "identifier",
                    "version",
                    "name",
                    "description",
                    "author",
                    "kind",
                    "download",
                    "download_size",
                    "homepage",
                    "contact"
                );

                foreach (var mod in registry.InstalledModules.OrderBy(i => i.Module.identifier))
                {
                    writer.WriteLine(WritePattern,
                        _delimter,
                        mod.Module.identifier,
                        mod.Module.modVersion,
                        QuoteIfNecessary(mod.Module.identifier),
                        QuoteIfNecessary(mod.Module.description),
                        QuoteIfNecessary(string.Join(";", mod.Module.authors)),
                        QuoteIfNecessary(mod.Module.kind.ToString()),
                        mod.Module.download != null ? WriteUri(mod.Module.download) : "",
                        mod.Module.download_size.ToString(),
                        mod.Module.homepage,
                        mod.Module.contact
                    );
                }
            }
        }

        private string WriteUri(Uri uri)
        {
            return uri != null ? QuoteIfNecessary(uri.ToString()) : string.Empty;
        }

        private string QuoteIfNecessary(string value)
        {
            if (value != null && value.IndexOf(_delimter, StringComparison.Ordinal) >= 0)
            {
                return "\"" + value + "\"";
            }
            else
            {
                return value;
            }
        }

        public enum Delimter
        {
            Comma,
            Tab
        }


    }
}
