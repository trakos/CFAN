using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ChinhDo.Transactions;
using CKAN.Factorio;
using CKAN.Factorio.Version;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using log4net;

namespace CKAN
{
    /// <summary>
    ///     Class for downloading the CKAN meta-info itself.
    /// </summary>
    public static class Repo
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (Repo));
        private static TxFileManager file_transaction = new TxFileManager();

        // Forward to keep existing code compiling, will be removed soon.
        public static readonly Uri default_ckan_repo = CKAN.Repository.default_ckan_repo_uri;

        internal static void ProcessRegistryMetadataFromJSON(string metadata, Registry registry, string filename)
        {
            log.DebugFormat("Converting metadata from JSON.");

            try
            {
                CfanModule module = CfanFileManager.fromJson(metadata);
                log.InfoFormat("Found {0} version {1}", module.identifier, module.modVersion);
                registry.AddAvailable(module);
            }
            catch (Exception exception)
            {
                // Alas, we can get exceptions which *wrap* our exceptions,
                // because json.net seems to enjoy wrapping rather than propagating.
                // See KSP-CKAN/CKAN-meta#182 as to why we need to walk the whole
                // exception stack.

                bool handled = false;

                while (exception != null)
                {
                    if (exception is UnsupportedKraken || exception is BadMetadataKraken)
                    {
                        // Either of these can be caused by data meant for future
                        // clients, so they're not really warnings, they're just
                        // informational.

                        log.InfoFormat("Skipping {0} : {1}", filename, exception.Message);

                        // I'd *love a way to "return" from the catch block.
                        handled = true;
                        break;
                    }

                    // Look further down the stack.
                    exception = exception.InnerException;
                }

                // If we haven't handled our exception, then it really was exceptional.
                if (handled == false)
                {
                    // In case whatever's calling us is lazy in error reporting, we'll
                    // report that we've got an issue here.
                    log.ErrorFormat("Error processing {0} : {1}", filename, exception.Message);
                    throw;
                }
            }
        }

        /// <summary>
        ///     Download and update the local CKAN meta-info.
        ///     Optionally takes a URL to the zipfile repo to download.
        ///     Returns the number of unique modules updated.
        /// </summary>
        public static int UpdateAllRepositories(RegistryManager registry_manager, KSP ksp, IUser user)
        {
            // If we handle multiple repositories, we will call ClearRegistry() ourselves...
            registry_manager.registry.ClearAvailable();
            // TODO this should already give us a pre-sorted list
            SortedDictionary<string, Repository> sortedRepositories = registry_manager.registry.Repositories;
            foreach (KeyValuePair<string, Repository> repository in sortedRepositories)
            {
                log.InfoFormat("About to update {0}", repository.Value.name);
                UpdateRegistry(repository.Value.uri, registry_manager.registry, ksp, user, false);
            }

            // Save our changes
            // we're not sure where it's called from (and whether InconsistentKraken will be handled), and it shouldn't change consistency, so let's not check for it.
            registry_manager.Save(false);

            // maybe we can move some preexisting modules to installed (i.e. they now may have corresponding version available)
            ksp.ScanGameData();

            // Return how many we got!
            return registry_manager.registry.Available(ksp.Version()).Count;
        }

        public static int Update(RegistryManager registry_manager, KSP ksp, IUser user, Boolean clear = true, Uri repo = null)
        {
            // Use our default repo, unless we've been told otherwise.
            if (repo == null)
            {
                repo = default_ckan_repo;
            }

            UpdateRegistry(repo, registry_manager.registry, ksp, user, clear);

            // Save our changes!
            registry_manager.Save();

            // maybe we can move some preexisting modules to installed (i.e. they now may have corresponding version available)
            ksp.ScanGameData();

            // Return how many we got!
            return registry_manager.registry.Available(ksp.Version()).Count;
        }

        public static int Update(RegistryManager registry_manager, KSP ksp, IUser user, Boolean clear = true, string repo = null)
        {
            if (repo == null)
            {
                return Update(registry_manager, ksp, user, clear, (Uri)null);
            }

            return Update(registry_manager, ksp, user, clear, new Uri(repo));
        }

        /// <summary>
        /// Updates the supplied registry from the URL given.
        /// This does not *save* the registry. For that, you probably want Repo.Update
        /// </summary>
        internal static void UpdateRegistry(Uri repo, Registry registry, KSP ksp, IUser user, Boolean clear = true)
        {
            log.InfoFormat("Downloading {0}", repo);

            string repo_file = String.Empty;
            try
            {
                repo_file = Net.Download(repo);
            }
            catch (System.Net.WebException e)
            {
                user.RaiseError($"Couldn't download {repo}.", e);
                return;
            }

            // Clear our list of known modules.
            var old_available = registry.available_modules;
            if (clear)
            {
                registry.ClearAvailable();
            }

            // Check the filetype.
            FileType type = FileIdentifier.IdentifyFile(repo_file);

            switch (type)
            {
            case FileType.TarGz:
                UpdateRegistryFromTarGz (repo_file, registry);
                break;
            case FileType.Zip:
                UpdateRegistryFromZip (repo_file, registry);
                break;
            default:
                break;
            }

            List<CfanModule> metadataChanges = new List<CfanModule>();

            foreach (var identifierModulePair in old_available)
            {
                var identifier = identifierModulePair.Key;

                if (registry.IsInstalled(identifier))
                {
                    AbstractVersion abstractVersion = registry.InstalledVersion(identifier);
                    var installedVersion = new ModVersion(abstractVersion.ToString());

                    if (!(registry.available_modules.ContainsKey(identifier)))
                    {
                        log.InfoFormat("UpdateRegistry, module {0}, version {1} not in repository ({2})", identifier, installedVersion, repo);
                        continue;
                    }

                    if (!registry.available_modules[identifier].module_version.ContainsKey(installedVersion))
                    {
                        continue;
                    }

                    // if the mod is installed and the metadata is different we have to reinstall it
                    CfanModule metadata = new CfanModule(registry.available_modules[identifier].module_version[installedVersion]);

                    if (!old_available.ContainsKey(identifier) ||
                        !old_available[identifier].module_version.ContainsKey(installedVersion))
                    {
                        continue;
                    }

                    CfanModule oldMetadata = new CfanModule(old_available[identifier].module_version[installedVersion]);

                    bool same = metadata.kind == oldMetadata.kind;

                    if (!same)
                    {
                        metadataChanges.Add(new CfanModule(registry.available_modules[identifier].module_version[installedVersion]));
                    }
                }
            }

            if (metadataChanges.Any())
            {
                string mods = "";
                for (int i = 0; i < metadataChanges.Count; i++)
                {
                    mods += metadataChanges[i].identifier + " "
                        + metadataChanges[i].modVersion.ToString() + ((i < metadataChanges.Count-1) ? ", " : "");
                }

                if(user.RaiseYesNoDialog(String.Format(
                    @"The following mods have had their metadata changed since last update - {0}.
It is advisable that you reinstall them in order to preserve consistency with the repository. Do you wish to reinstall now?", mods)))
                {
                    ModuleInstaller installer = ModuleInstaller.GetInstance(ksp, new NullUser());
                    installer.Upgrade(metadataChanges, new NetAsyncModulesDownloader(new NullUser(), ksp.tryGetFactorioAuthData()));
                }
            }

            // Remove our downloaded meta-data now we've processed it.
            // Seems weird to do this as part of a transaction, but Net.Download uses them, so let's be consistent.
            file_transaction.Delete(repo_file);
        }

        /// <summary>
        /// Updates the supplied registry from the supplied zip file.
        /// This will *clear* the registry of available modules first.
        /// This does not *save* the registry. For that, you probably want Repo.Update
        /// </summary>
        internal static void UpdateRegistryFromTarGz(string path, Registry registry)
        {
            log.DebugFormat("Starting registry update from tar.gz file: \"{0}\".", path);

            // Open the gzip'ed file.
            using (Stream inputStream = File.OpenRead(path))
            {
                // Create a gzip stream.
                using (GZipInputStream gzipStream = new GZipInputStream(inputStream))
                {
                    // Create a handle for the tar stream.
                    using (TarInputStream tarStream = new TarInputStream(gzipStream))
                    {
                        // Walk the archive, looking for .ckan files.
                        const string filter = @"\.cfan$";

                        while (true)
                        {
                            TarEntry entry = tarStream.GetNextEntry();

                            // Check for EOF.
                            if (entry == null)
                            {
                                break;
                            }

                            string filename = entry.Name;

                            // Skip things we don't want.
                            if (!Regex.IsMatch(filename, filter))
                            {
                                log.DebugFormat("Skipping archive entry {0}", filename);
                                continue;
                            }

                            log.DebugFormat("Reading CFAN data from {0}", filename);

                            // Read each file into a buffer.
                            int buffer_size;

                            try
                            {
                                buffer_size = Convert.ToInt32(entry.Size);
                            }
                            catch (OverflowException)
                            {
                                log.ErrorFormat("Error processing {0}: Metadata size too large.", entry.Name);
                                continue;
                            }

                            byte[] buffer = new byte[buffer_size];

                            tarStream.Read(buffer, 0, buffer_size);

                            // Convert the buffer data to a string.
                            string metadata_json = Encoding.ASCII.GetString(buffer);

                            ProcessRegistryMetadataFromJSON(metadata_json, registry, filename);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the supplied registry from the supplied zip file.
        /// This will *clear* the registry of available modules first.
        /// This does not *save* the registry. For that, you probably want Repo.Update
        /// </summary>
        internal static void UpdateRegistryFromZip(string path, Registry registry)
        {
            log.DebugFormat("Starting registry update from zip file: \"{0}\".", path);

            using (var zipfile = new ZipFile(path))
            {
                // Walk the archive, looking for .ckan files.
                const string filter = @"\.cfan$";

                foreach (ZipEntry entry in zipfile)
                {
                    string filename = entry.Name;

                    // Skip things we don't want.
                    if (! Regex.IsMatch(filename, filter))
                    {
                        log.DebugFormat("Skipping archive entry {0}", filename);
                        continue;
                    }

                    log.DebugFormat("Reading CFAN data from {0}", filename);

                    // Read each file into a string.
                    string metadata_json;
                    using (var stream = new StreamReader(zipfile.GetInputStream(entry)))
                    {
                        metadata_json = stream.ReadToEnd();
                        stream.Close();
                    }

                    ProcessRegistryMetadataFromJSON(metadata_json, registry, filename);
                }

                zipfile.Close();
            }
        }
    }
}