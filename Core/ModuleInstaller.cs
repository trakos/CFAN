using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using ChinhDo.Transactions;
using CKAN.Factorio;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;
using CKAN.Installable;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using log4net;

namespace CKAN
{
    public delegate void ModuleInstallerReportModInstalled(CfanModule module);

    public class ModuleInstaller
    {
        public IUser User { get; set; }

        // To allow the ModuleInstaller to work on multiple KSP instances, keep a list of each ModuleInstaller and return the correct one upon request.
        private static SortedList<string, ModuleInstaller> instances = new SortedList<string, ModuleInstaller>();

        private static readonly ILog log = LogManager.GetLogger(typeof(ModuleInstaller));
        private static readonly TxFileManager file_transaction = new TxFileManager ();

        private RegistryManager registry_manager;
        private KSP ksp;

        public ModuleInstallerReportModInstalled onReportModInstalled = null;

        // Our own cache is that of the KSP instance we're using.
        public NetFileCache Cache
        {
            get
            {
                return ksp.Cache;
            }
        }

        // Constructor
        private ModuleInstaller(KSP ksp, IUser user)
        {
            User = user;
            this.ksp = ksp;
            registry_manager = RegistryManager.Instance(ksp);
            log.DebugFormat("Creating ModuleInstaller for {0}", ksp.GameDir());
        }

        /// <summary>
        /// Gets the ModuleInstaller instance associated with the passed KSP instance. Creates a new ModuleInstaller instance if none exists.
        /// </summary>
        /// <returns>The ModuleInstaller instance.</returns>
        /// <param name="ksp_instance">Current KSP instance.</param>
        /// <param name="user">IUser implementation.</param>
        public static ModuleInstaller GetInstance(KSP ksp_instance, IUser user)
        {
            ModuleInstaller instance;

            // Check in the list of instances if we have already created a ModuleInstaller instance for this KSP instance.
            if (!instances.TryGetValue(ksp_instance.GameDir().ToLower(), out instance))
            {
                // Create a new instance and insert it in the static list.
                instance = new ModuleInstaller(ksp_instance, user);

                instances.Add(ksp_instance.GameDir().ToLower(), instance);
            }

            return instance;
        }

        /// <summary>
        /// Downloads the given mod to the cache. Returns the filename it was saved to.
        /// </summary>
        public string Download(Uri url, string filename)
        {
            User.RaiseProgress(String.Format("Downloading \"{0}\"", url), 0);
            return Download(url, filename, Cache);
        }

        /// <summary>
        /// Downloads the given mod to the cache. Returns the filename it was saved to.
        /// </summary>
        public static string Download(Uri url, string filename, NetFileCache cache)
        {
            log.Info("Downloading " + filename);

            string tmp_file = Net.Download(url);

            return cache.Store(url, tmp_file, filename, true);
        }

        /// <summary>
        /// Returns the path to a cached copy of a module if it exists, or downloads
        /// and returns the downloaded copy otherwise.
        ///
        /// If no filename is provided, the module's standard name will be used.
        /// Chcecks the CKAN cache first.
        /// </summary>
        public string CachedOrDownload(CfanModule module, string filename = null)
        {
            return CachedOrDownload(module.identifier, module.modVersion, module.download, Cache, filename);
        }

        /// <summary>
        /// Returns the path to a cached copy of a module if it exists, or downloads
        /// and returns the downloaded copy otherwise.
        ///
        /// If no filename is provided, the module's standard name will be used.
        /// Chcecks the CKAN cache first.
        /// </summary>
        public string CachedOrDownload(string identifier, AbstractVersion version, Uri url, string filename = null)
        {
            return CachedOrDownload(identifier, version, url, Cache, filename);
        }

        /// <summary>
        /// Returns the path to a cached copy of a module if it exists, or downloads
        /// and returns the downloaded copy otherwise.
        ///
        /// If no filename is provided, the module's standard name will be used.
        /// Chcecks provided cache first.
        /// </summary>
        public static string CachedOrDownload(string identifier, AbstractVersion version, Uri url, NetFileCache cache, string filename = null)
        {
            if (filename == null)
            {
                filename = CfanModule.createStandardFileName(identifier, version.ToString());
            }

            string full_path = cache.GetCachedZip(url);
            if (full_path == null)
            {
                return Download(url, filename, cache);
            }

            log.DebugFormat("Using {0} (cached)", filename);
            return full_path;
        }



        public void InstallList(
            IEnumerable<CfanModuleIdAndVersion> modules,
            RelationshipResolverOptions options,
            IDownloader downloader = null
        )
        {
            var resolver = new RelationshipResolver(modules, options, registry_manager.registry, ksp.Version());
            List<CfanModule> modsToInstall = resolver.ModList();

            InstallList(modsToInstall, options, downloader);
        }

        /// <summary>
        ///     Installs all modules given a list of identifiers as a transaction. Resolves dependencies.
        ///     This *will* save the registry at the end of operation.
        ///
        /// Propagates a BadMetadataKraken if our install metadata is bad.
        /// Propagates a FileExistsKraken if we were going to overwrite a file.
        /// Propagates a CancelledActionKraken if the user cancelled the install.
        /// </summary>
        //
        // TODO: Break this up into smaller pieces! It's huge!
        public void InstallList(
            ICollection<CfanModule> modules,
            RelationshipResolverOptions options,
            IDownloader downloader = null
        )
        {
            var resolver = new RelationshipResolver(modules, options, registry_manager.registry, ksp.Version());
            List<CfanModule> modsToInstall = resolver.ModList();
            List<CfanModule> downloads = new List<CfanModule> ();

            // TODO: All this user-stuff should be happening in another method!
            // We should just be installing mods as a transaction.

            User.RaiseMessage("About to install...\n");

            foreach (CfanModule module in modsToInstall)
            {
                if (!ksp.Cache.IsCachedZip(module.download))
                {
                    User.RaiseMessage(" * {0} {1}", module.identifier, module.modVersion);
                    downloads.Add(module);
                }
                else
                {
                    User.RaiseMessage(" * {0} {1}(cached)", module.identifier, module.modVersion);
                }
            }

            bool ok = User.RaiseYesNoDialog("\nContinue?");

            if (!ok)
            {
                throw new CancelledActionKraken("User declined install list");
            }

            User.RaiseMessage(String.Empty); // Just to look tidy.

            if (downloads.Count > 0)
            {
                if (downloader == null)
                {
                    downloader = new NetAsyncModulesDownloader(User, ksp.tryGetFactorioAuthData());
                }

                downloader.DownloadModules(ksp.Cache, downloads);
            }

            // We're about to install all our mods; so begin our transaction.
            using (TransactionScope transaction = CkanTransaction.CreateTransactionScope())
            {
                for (int i = 0; i < modsToInstall.Count; i++)
                {
                    int percent_complete = (i * 100) / modsToInstall.Count;

                    User.RaiseProgress(String.Format("Installing mod \"{0}\"", modsToInstall[i]),
                                         percent_complete);

                    Install(modsToInstall[i]);
                }

                User.RaiseProgress("Updating registry", 70);

                registry_manager.Save(!options.without_enforce_consistency);
                ksp.RebuildFactorioModlist();

                User.RaiseProgress("Commiting filesystem changes", 80);

                transaction.Complete();

            }

            // We can scan GameData as a separate transaction. Installing the mods
            // leaves everything consistent, and this is just gravy. (And ScanGameData
            // acts as a Tx, anyway, so we don't need to provide our own.)

            User.RaiseProgress("Rescanning GameData", 90);

            if (!options.without_enforce_consistency)
            {
                ksp.ScanGameData();
            }

            User.RaiseProgress("Done!\n", 100);
        }

        /// <summary>
        /// Returns the module contents if and only if we have it
        /// available in our cache. Returns null, otherwise.
        ///
        /// Intended for previews.
        /// </summary>
        // TODO: Return files relative to GameRoot
        public IEnumerable<string> GetModuleContentsList(CfanModule module)
        {
            string filename = ksp.Cache.GetCachedZip(module.download);

            if (filename == null)
            {
                return null;
            }

            return FindInstallableFiles(module, filename, ksp)
                .Select(x => KSPPathUtils.NormalizePath(x.Destination));
        }

        /// <summary>
        ///     Install our mod from the filename supplied.
        ///     If no file is supplied, we will check the cache or throw FileNotFoundKraken.
        ///     Does *not* resolve dependencies; this actually does the heavy listing.
        ///     Does *not* save the registry.
        ///     Do *not* call this directly, use InstallList() instead.
        ///
        /// Propagates a BadMetadataKraken if our install metadata is bad.
        /// Propagates a FileExistsKraken if we were going to overwrite a file.
        /// Throws a FileNotFoundKraken if we can't find the downloaded module.
        ///
        /// </summary>
        //
        // TODO: The name of this and InstallModule() need to be made more distinctive.

        private void Install(CfanModule module, string filename = null)
        {
            CheckMetapackageInstallationKraken(module);

            AbstractVersion version = registry_manager.registry.InstalledVersion(module.identifier);

            // TODO: This really should be handled by higher-up code.
            if (version != null)
            {
                User.RaiseMessage("    {0} {1} already installed, skipped", module.identifier, version);
                return;
            }

            // Find our in the cache if we don't already have it.
            filename = filename ?? Cache.GetCachedZip(module.download,true);

            // If we *still* don't have a file, then kraken bitterly.
            if (filename == null)
            {
                throw new FileNotFoundKraken(
                    null,
                    String.Format("Trying to install {0}, but it's not downloaded or download is corrupted", module)
                );
            }

            // We'll need our registry to record which files we've installed.
            Registry registry = registry_manager.registry;

            using (var transaction = CkanTransaction.CreateTransactionScope())
            {
                // Install all the things!
                IEnumerable<string> files = InstallModule(module, filename);

                // Register our module and its files.
                registry.RegisterModule(module, files, ksp);

                // Finish our transaction, but *don't* save the registry; we may be in an
                // intermediate, inconsistent state.
                // This is fine from a transaction standpoint, as we may not have an enclosing
                // transaction, and if we do, they can always roll us back.
                transaction.Complete();
            }

            // Fire our callback that we've installed a module, if we have one.
            if (onReportModInstalled != null)
            {
                onReportModInstalled(module);
            }

        }

        /// <summary>
        /// Check if the given module is a metapackage:
        /// if it is, throws a BadCommandKraken.
        /// </summary>
        private static void CheckMetapackageInstallationKraken(CfanModule module)
        {
            if (module.isMetapackage)
            {
                throw new BadCommandKraken("Metapackages can not be installed!");
            }
        }

        /// <summary>
        /// Installs the module from the zipfile provided.
        /// Returns a list of files installed.
        /// Propagates a BadMetadataKraken if our install metadata is bad.
        /// Propagates a FileExistsKraken if we were going to overwrite a file.
        /// </summary>
        private IEnumerable<string> InstallModule(CfanModule module, string zip_filename)
        {
            CheckMetapackageInstallationKraken(module);
            
            IEnumerable<IInstallable> files = FindInstallableFiles(module, zip_filename, ksp);

            try
            {
                foreach (IInstallable file in files)
                {
                    log.InfoFormat("Copying {0}", file.Name);
                    CopyZipEntry(ksp.getModTypeRootDirectory(module.kind), file);
                }
            }
            catch (FileExistsKraken kraken)
            {
                // Decorate the kraken with our module and re-throw
                kraken.filename = ksp.ToRelativeGameDataDir(kraken.filename);
                kraken.installing_module = module;
                kraken.owning_module = registry_manager.registry.FileOwner(kraken.filename);
                throw;
            }

            return files.Select(x => Path.Combine(ksp.getModTypeRootDirectory(module.kind), x.Destination));
        }

        /// <summary>
        /// Given a module and a path to a zipfile, returns all the files that would be installed
        /// from that zip for this module.
        ///
        /// This *will* throw an exception if the file does not exist.
        ///
        /// Throws a BadMetadataKraken if the stanza resulted in no files being returned.
        ///
        /// If a KSP instance is provided, it will be used to generate output paths, otherwise these will be null.
        /// </summary>
        // TODO: Document which exception!
        public static List<IInstallable> FindInstallableFiles(CfanModule module, string zip_filename, KSP ksp)
        {
            if (module.kind == CfanJson.CfanModType.META)
            {
                return new List<IInstallable>();
            }
            if (module.kind != CfanJson.CfanModType.MOD)
            {
                throw new NotImplementedException("Only regular mod is implemented.");
            }
            return new List<IInstallable>(new IInstallable[] { new InstallableFile(zip_filename, module.standardFileName + ".zip")});
        }

        /// <summary>
        /// Copy the entry from the opened zipfile to the path specified.
        /// </summary>
        internal static void CopyZipEntry(string absoluteDirectoryRoot, IInstallable file)
        {
            string absolutePath = Path.Combine(absoluteDirectoryRoot, file.Destination);

            if (file.IsDirectory)
            {
                // Skip if we're not making directories for this install.
                if (!file.makeDirs)
                {
                    log.DebugFormat ("Skipping {0}, we don't make directories for this path", absolutePath);
                    return;
                }

                log.DebugFormat("Making directory {0}", absolutePath);
                file_transaction.CreateDirectory(absolutePath);
            }
            else
            {
                log.DebugFormat("Writing file {0}", absolutePath);

                // Sometimes there are zipfiles that don't contain entries for the
                // directories their files are in. No, I understand either, but
                // the result is we have to make sure our directories exist, just in case.
                if (file.makeDirs)
                {
                    string directory = Path.GetDirectoryName(absolutePath);
                    file_transaction.CreateDirectory(directory);
                }

                // We don't allow for the overwriting of files. See #208.
                if (File.Exists(absolutePath))
                {
                    throw new FileExistsKraken(absolutePath, string.Format("Trying to write {0} but it already exists.", absolutePath));
                }

                // Snapshot whatever was there before. If there's nothing, this will just
                // remove our file on rollback. We still need this even thought we won't
                // overwite files, as it ensures deletiion on rollback.
                file_transaction.Snapshot(absolutePath);

                try
                {
                    // It's a file! Prepare the streams
                    using (Stream zipStream = file.stream)
                    using (FileStream writer = File.Create(absolutePath))
                    {
                        // 4k is the block size on practically every disk and OS.
                        byte[] buffer = new byte[4096];
                        StreamUtils.Copy(zipStream, writer, buffer);
                    }
                }
                catch (DirectoryNotFoundException ex)
                {
                    throw new DirectoryNotFoundKraken("", ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// Uninstalls all the mods provided, including things which depend upon them.
        /// This *DOES* save the registry.
        /// Preferred over Uninstall.
        /// </summary>
        public void UninstallList(IEnumerable<string> mods)
        {
            // Pre-check, have they even asked for things which are installed?

            foreach (string mod in mods.Where(mod => registry_manager.registry.InstalledModule(mod) == null))
            {
                throw new ModNotInstalledKraken(mod);
            }

            // Find all the things which need uninstalling.
            IEnumerable<string> goners = registry_manager.registry.FindReverseDependencies(mods);

            // If there us nothing to uninstall, skip out.
            if (!goners.Any())
            {
                return;
            }

            User.RaiseMessage("About to remove:\n");

            foreach (string mod in goners)
            {
                InstalledModule module = registry_manager.registry.InstalledModule(mod);
                User.RaiseMessage(" * {0} {1}", module.Module.identifier, module.Module.modVersion);
            }

            bool ok = User.RaiseYesNoDialog("\nContinue?");

            if (!ok)
            {
                User.RaiseMessage("Mod removal aborted at user request.");
                return;
            }

            using (var transaction = CkanTransaction.CreateTransactionScope())
            {
                foreach (string mod in goners)
                {
                    User.RaiseMessage("Removing {0}...", mod);
                    Uninstall(mod);
                }

                registry_manager.Save();
                ksp.RebuildFactorioModlist();

                transaction.Complete();
            }

            User.RaiseMessage("Done!\n");
        }

        public void UninstallList(string mod)
        {
            var list = new List<string> {mod};
            UninstallList(list);
        }

        /// <summary>
        /// Uninstall the module provided. For internal use only.
        /// Use UninstallList for user queries, it also does dependency handling.
        /// This does *NOT* save the registry.
        /// </summary>

        private void Uninstall(string modName)
        {
            using (var transaction = CkanTransaction.CreateTransactionScope())
            {
                InstalledModule mod = registry_manager.registry.InstalledModule(modName);

                if (mod == null)
                {
                    log.ErrorFormat("Trying to uninstall {0} but it's not installed", modName);
                    throw new ModNotInstalledKraken(modName);
                }

                // Walk our registry to find all files for this mod.
                IEnumerable<string> files = mod.Files;

                var directoriesToDelete = new HashSet<string>();

                foreach (string file in files)
                {
                    string path = ksp.ToAbsoluteGameDataDir(file);

                    try
                    {
                        FileAttributes attr = File.GetAttributes(path);

                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            directoriesToDelete.Add(path);
                        }
                        else
                        {
                            log.InfoFormat("Removing {0}", file);
                            file_transaction.Delete(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        // XXX: This is terrible, we're catching all exceptions.
                        log.ErrorFormat("Failure in locating file {0} : {1}", path, ex.Message);
                    }
                }

                // Remove from registry.

                registry_manager.registry.DeregisterModule(ksp, modName);

                // Sort our directories from longest to shortest, to make sure we remove child directories
                // before parents. GH #78.
                foreach (string directory in directoriesToDelete.OrderBy(dir => dir.Length).Reverse())
                {
                    if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        // It is bad if any of this directories get's removed
                        // So we protect them
                        if (directory == ksp.Scenarios() || directory == ksp.GameData()
                            || directory == ksp.GameDir() || directory == ksp.CkanDir()
                            || directory == ksp.Mods())
                        {
                            continue;
                        }

                        // We *don't* use our file_transaction to delete files here, because
                        // it fails if the system's temp directory is on a different device
                        // to KSP. However we *can* safely delete it now we know it's empty,
                        // because the TxFileMgr *will* put it back if there's a file inside that
                        // needs it.
                        //
                        // This works around GH #251.
                        // The filesystem boundry bug is described in https://transactionalfilemgr.codeplex.com/workitem/20

                        log.InfoFormat("Removing {0}", directory);
                        Directory.Delete(directory);
                    }
                    else
                    {
                        log.InfoFormat("Not removing directory {0}, it's not empty", directory);
                    }
                }
                transaction.Complete();
            }
        }

        #region AddRemove

        /// <summary>
        /// Adds and removes the listed modules as a single transaction.
        /// No relationships will be processed.
        /// This *will* save the registry.
        /// </summary>
        /// <param name="add">Add.</param>
        /// <param name="remove">Remove.</param>
        public void AddRemove(IEnumerable<CfanModule> add = null, IEnumerable<string> remove = null)
        {

            // TODO: We should do a consistency check up-front, rather than relying
            // upon our registry catching inconsistencies at the end.

            using (var tx = CkanTransaction.CreateTransactionScope())
            {

                foreach (string identifier in remove)
                {
                    Uninstall(identifier);
                }

                foreach (CfanModule module in add)
                {
                    Install(module);
                }

                registry_manager.Save();
                ksp.RebuildFactorioModlist();

                tx.Complete();
            }
        }

        /// <summary>
        /// Upgrades the mods listed to the latest versions for the user's KSP.
        /// Will *re-install* with warning even if an upgrade is not available.
        /// Throws ModuleNotFoundKraken if module is not installed, or not available.
        /// </summary>
        public void Upgrade(IEnumerable<string> identifiers, IDownloader netAsyncDownloader)
        {
            var options = new RelationshipResolverOptions();

            // We do not wish to pull in any suggested or recommended mods.
            options.with_recommends = false;
            options.with_suggests = false;

            List<CfanModuleIdAndVersion> moduleNames = identifiers.Select(p => new CfanModuleIdAndVersion(p)).ToList();
            var resolver = new RelationshipResolver(moduleNames, options, registry_manager.registry, ksp.Version());
            List<CfanModule> upgrades = resolver.ModList();

            Upgrade(upgrades, netAsyncDownloader);
        }

        /// <summary>
        /// Upgrades or installs the mods listed to the specified versions for the user's KSP.
        /// Will *re-install* or *downgrade* (with a warning) as well as upgrade.
        /// Throws ModuleNotFoundKraken if a module is not installed.
        /// </summary>
        public void Upgrade(IEnumerable<CfanModule> modules, IDownloader netAsyncDownloader)
        {
            // Start by making sure we've downloaded everything.
            DownloadModules(modules, netAsyncDownloader);

            // Our upgrade involves removing everything that's currently installed, then
            // adding everything that needs installing (which may involve new mods to
            // satisfy dependencies). We always know the list passed in is what we need to
            // install, but we need to calculate what needs to be removed.
            var to_remove = new List<string>();

            // Let's discover what we need to do with each module!
            foreach (CfanModule module in modules)
            {
                string ident = module.identifier;
                InstalledModule installed_mod = registry_manager.registry.InstalledModule(ident);

                if (installed_mod == null)
                {
                    //Maybe ModuleNotInstalled ?
                    if (registry_manager.registry.IsAutodetected(ident))
                    {
                        throw new ModuleNotFoundKraken(ident, module.modVersion.ToString(), String.Format("Can't upgrade {0} as it was not installed by CFAN. \n Please remove manually before trying to install it.", ident));
                    }

                    User.RaiseMessage("Installing previously uninstalled mod {0}", ident);
                }
                else
                {
                    // Module already installed. We'll need to remove it first.
                    to_remove.Add(module.identifier);

                    CfanModule installed = installed_mod.Module;
                    if (installed.modVersion.Equals(module.modVersion))
                    {
                        log.WarnFormat("{0} is already at the latest version, reinstalling", installed.identifier);
                    }
                    else if (installed.modVersion.IsGreaterThan(module.modVersion))
                    {
                        log.WarnFormat("Downgrading {0} from {1} to {2}", ident, installed.modVersion, module.modVersion);
                    }
                    else
                    {
                        log.InfoFormat("Upgrading {0} to {1}", ident, module.modVersion);
                    }
                }
            }

            AddRemove(
                modules,
                to_remove
            );
        }

        #endregion

        /// <summary>
        /// Makes sure all the specified mods are downloaded.
        /// </summary>
        private void DownloadModules(IEnumerable<CfanModule> mods, IDownloader downloader)
        {
            List<CfanModule> downloads = mods.Where(module => !ksp.Cache.IsCachedZip(module.download)).ToList();

            if (downloads.Count > 0)
            {
                downloader.DownloadModules(ksp.Cache, downloads);
            }
        }
    }
}

