﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CKAN.Exporters;
using CKAN.Factorio;
using CKAN.Factorio.Version;
using CKAN.Types;
using log4net;

namespace CKAN.CmdLine
{
    public class List : ICommand
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(List));

        public IUser user { get; set; }

        public List(IUser user)
        {
            this.user = user;
        }

        public int RunCommand(CKAN.KSP ksp, object raw_options)
        {
            ListOptions options = (ListOptions) raw_options;

            IRegistryQuerier registry = RegistryManager.Instance(ksp).registry;


            ExportFileType? exportFileType = null;

            if (!string.IsNullOrWhiteSpace(options.export))
            {
                exportFileType = GetExportFileType(options.export);

                if (exportFileType == null)
                {
                    user.RaiseError("Unknown export format: {0}", options.export);
                }
            }

            if (!(options.porcelain) && exportFileType == null)
            {
                user.RaiseMessage("\nFactorio found at {0}\n", ksp.GameDir());
                user.RaiseMessage("Factorio Version: {0}\n", ksp.Version());

                user.RaiseMessage("Installed Modules:\n");
            }

            if (exportFileType == null)
            {
                var installed = new SortedDictionary<string, AbstractVersion>(registry.Installed());

                foreach (KeyValuePair<string, AbstractVersion> mod in installed)
                {
                    AbstractVersion current_version = mod.Value;

                    string bullet = "*";

                    if (current_version is ProvidedVersion)
                    {
                        // Skip virtuals for now.
                        continue;
                    }
                    else if (current_version is AutodetectedVersion)
                    {
                        // Autodetected dll
                        bullet = "-";
                    }
                    else
                    {
                        try
                        {
                            // Check if upgrades are available, and show appropriately.
                            CfanModule latest = registry.LatestAvailable(mod.Key, ksp.Version());

                            log.InfoFormat("Latest {0} is {1}", mod.Key, latest);

                            if (latest == null)
                            {
                                // Not compatible!
                                bullet = "X";
                            }
                            else if (latest.modVersion.Equals(current_version))
                            {
                                // Up to date
                                bullet = "-";
                            }
                            else if (latest.modVersion.IsGreaterThan(mod.Value))
                            {
                                // Upgradable
                                bullet = "^";
                            }
                        }
                        catch (ModuleNotFoundKraken)
                        {
                            log.InfoFormat("{0} is installed, but no longer in the registry", mod.Key);
                            bullet = "?";
                        }
                    }

                    user.RaiseMessage("{0} {1} {2}", bullet, mod.Key, mod.Value);
                }
            }
            else
            {
                var stream = Console.OpenStandardOutput();
                new Exporter(exportFileType.Value).Export(registry, stream);
                stream.Flush();
            }

            if (!(options.porcelain) && exportFileType == null)
            {
                user.RaiseMessage("\nLegend: -: Up to date. X: Incompatible. ^: Upgradable. ?: Unknown ");
            }

            return Exit.OK;
        }

        private static ExportFileType? GetExportFileType(string export)
        {
            export = export.ToLowerInvariant();

            switch (export)
            {
                case "text":
                    return ExportFileType.PlainText;
                case "markdown":
                    return ExportFileType.Markdown;
                case "bbcode":
                    return ExportFileType.BbCode;
                case "csv":
                    return ExportFileType.Csv;
                case "tsv":
                    return ExportFileType.Tsv;
                default:
                    return null;
            }
        }
    }
}

