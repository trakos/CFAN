using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CKAN.Factorio;
using CKAN.Factorio.Relationships;

namespace CKAN.CmdLine
{
    public class Show : ICommand
    {
        public IUser user { get; set; }

        public Show(IUser user)
        {
            this.user = user;
        }

        public int RunCommand(CKAN.KSP ksp, object raw_options)
        {
            ShowOptions options = (ShowOptions) raw_options;

            if (options.Modname == null)
            {
                // empty argument
                user.RaiseMessage("show <module> - module name argument missing, perhaps you forgot it?");
                return Exit.BADOPT;
            }

            // Check installed modules for an exact match.
            InstalledModule installedModuleToShow = ksp.Registry.InstalledModule(options.Modname);

            if (installedModuleToShow != null)
            {
                // Show the installed module.
                return ShowMod(installedModuleToShow);
            }

            // Module was not installed, look for an exact match in the available modules,
            // either by "name" (the user-friendly display name) or by identifier
            CfanModule moduleToShow = ksp.Registry                  
                                      .Available(ksp.Version())
                                      .SingleOrDefault(
                                            mod => mod.title == options.Modname
                                                || mod.identifier == options.Modname
                                      );

            if (moduleToShow == null)
            {
                // No exact match found. Try to look for a close match for this KSP version.
                user.RaiseMessage("{0} not found or installed.", options.Modname);
                user.RaiseMessage("Looking for close matches in available mods for KSP {0}.", ksp.Version());

                Search search = new Search(user);
                List<CfanModule> matches = search.PerformSearch(ksp, options.Modname);

                // Display the results of the search.
                if (matches.Count == 0)
                {
                    // No matches found.
                    user.RaiseMessage("No close matches found.");
                    return Exit.BADOPT;
                }
                else if (matches.Count == 1)
                {
                    // If there is only 1 match, display it.
                    user.RaiseMessage("Found 1 close match: {0}", matches[0].title);
                    user.RaiseMessage("");

                    moduleToShow = matches[0];
                }
                else
                {
                    // Display the found close matches.
                    string[] strings_matches = new string[matches.Count];

                    for (int i = 0; i < matches.Count; i++)
                    {
                        strings_matches[i] = matches[i].title;
                    }

                    int selection = user.RaiseSelectionDialog("Close matches", strings_matches);

                    if (selection < 0)
                    {
                        return Exit.BADOPT;
                    }

                    // Mark the selection as the one to show.
                    moduleToShow = matches[selection];
                }
            }

            return ShowMod(moduleToShow);
        }

        /// <summary>
        /// Shows information about the mod.
        /// </summary>
        /// <returns>Success status.</returns>
        /// <param name="module">The module to show.</param>
        public int ShowMod(InstalledModule module)
        {
            // Display the basic info.
            int return_value = ShowMod(module.Module);

            // Display InstalledModule specific information.
            ICollection<string> files = module.Files as ICollection<string>;
            if (files == null) throw new InvalidCastException();

            user.RaiseMessage("\nShowing {0} installed files:", files.Count);
            foreach (string file in files)
            {
                user.RaiseMessage("- {0}", file);
            }

            return return_value;
        }

        /// <summary>
        /// Shows information about the mod.
        /// </summary>
        /// <returns>Success status.</returns>
        /// <param name="module">The module to show.</param>
        public int ShowMod(CfanModule module)
        {
            #region Abstract and description
            user.RaiseMessage("{0}", module.title);

            if (!string.IsNullOrEmpty(module.description))
            {
                user.RaiseMessage("\n{0}\n", module.description);
            }
            #endregion

            #region General info (author, version...)
            user.RaiseMessage("\nModule info:");
            user.RaiseMessage("- version:\t{0}", module.modVersion);

            if (module.authors != null)
            {
                user.RaiseMessage("- authors:\t{0}", string.Join(", ", module.authors));
            }
            else
            {
                // Did you know that authors are optional in the spec?
                // You do now. #673.
                user.RaiseMessage("- authors:\tUNKNOWN");
            }
            
            #endregion

            #region Relationships
            if (module.depends != null && module.depends.Any())
            {
                user.RaiseMessage("\nDepends:");
                foreach (var dep in module.depends)
                    user.RaiseMessage("- {0}", RelationshipToPrintableString(dep));
            }

            if (module.recommends != null && module.recommends.Any())
            {
                user.RaiseMessage("\nRecommends:");
                foreach (var dep in module.recommends)
                    user.RaiseMessage("- {0}", RelationshipToPrintableString(dep));
            }

            if (module.suggests != null && module.suggests.Any())
            {
                user.RaiseMessage("\nSuggests:");
                foreach (var dep in module.suggests)
                    user.RaiseMessage("- {0}", RelationshipToPrintableString(dep));
            }

            if (module.providesNames != null && module.providesNames.Any())
            {
                user.RaiseMessage("\nProvides:");
                foreach (string prov in module.providesNames)
                    user.RaiseMessage("- {0}", prov);
            } 
            #endregion

            user.RaiseMessage("\nResources:");
            if (!String.IsNullOrEmpty(module.homepage))
                user.RaiseMessage("- homepage: {0}", Uri.EscapeUriString(module.homepage));
            if (!String.IsNullOrEmpty(module.contact))
                user.RaiseMessage("- contact: {0}", Uri.EscapeUriString(module.contact));

            if (!module.isMetapackage)
            {
                // Compute the CKAN filename.
                string file_uri_hash = NetFileCache.CreateURLHash(module.download);
                string file_name = CfanModule.createStandardFileName(module.identifier, module.modVersion.ToString());

                user.RaiseMessage("\nFilename: {0}", file_uri_hash + "-" + file_name);
            }

            return Exit.OK;
        }

        /// <summary>
        /// Formats a RelationshipDescriptor into a user-readable string:
        /// Name, version: x, min: x, max: x
        /// </summary>
        private static string RelationshipToPrintableString(ModDependency dep)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(dep.modName);
            if (dep.isOptional) sb.Append(" (only if installed)");
            if (dep.minVersion != null) sb.Append(", min: " + dep.minVersion);
            if (dep.maxVersion != null) sb.Append(", max: " + dep.maxVersion);
            return sb.ToString();
        }
    }
}

