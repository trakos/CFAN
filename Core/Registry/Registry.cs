using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Transactions;
using CKAN.Factorio;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;
using log4net;
using Newtonsoft.Json;

namespace CKAN
{
    /// <summary>
    /// This is the CKAN registry. All the modules that we know about or have installed
    /// are contained in here.
    /// </summary>

    // TODO: It would be *great* for the registry to have a 'dirty' bit, that records if
    // anything has changed. But that would involve catching access to a lot of the data
    // structures we pass back, and we're not doing that yet.

    public class Registry : IEnlistmentNotification, IRegistryQuerier
    {
        [JsonIgnore] private const int LATEST_REGISTRY_VERSION = 3;
        [JsonIgnore] private static readonly ILog log = LogManager.GetLogger(typeof (Registry));

#pragma warning disable 414
        // ReSharper disable once InconsistentNaming
        [JsonProperty] private int registry_version;
#pragma warning restore 414

        [JsonProperty("sorted_repositories")]
        private SortedDictionary<string, Repository> repositories; // name => Repository

        // TODO: These may be good as custom types, especially those which process
        // paths (and flip from absolute to relative, and vice-versa).
        [JsonProperty] internal Dictionary<string, AvailableModule> available_modules;
        [JsonProperty] private Dictionary<string, AutodetectedModule> installed_preexisting_modules; // name => mod info
        [JsonProperty] private Dictionary<string, InstalledModule> installed_modules;
        [JsonProperty] private Dictionary<string, string> installed_files; // filename => module

        [JsonIgnore] private string transaction_backup;

        /// <summary>
        /// Returns all the activated registries, sorted by priority and name
        /// </summary>
        [JsonIgnore] public SortedDictionary<string, Repository> Repositories
        {
            get { return this.repositories; }

            // TODO writable only so it can be initialized, better ideas welcome
            set { this.repositories = value; }
        }

        /// <summary>
        /// Returns all the installed modules
        /// </summary>
        [JsonIgnore] public IEnumerable<InstalledModule> InstalledModules
        {
            get { return installed_modules.Values; }
        }

        /// <summary>
        /// Returns the names of installed DLLs.
        /// </summary>
        [JsonIgnore] public IEnumerable<string> InstalledPreexistingModules
        {
            get { return installed_preexisting_modules.Keys; }
        }

        #region Registry Upgrades

        [OnDeserialized]
        private void DeSerialisationFixes(StreamingContext context)
        {
            // Our context is our KSP install.
            KSP ksp = (KSP) context.Context;
            registry_version = LATEST_REGISTRY_VERSION;
        }

        /// <summary>
        /// Rebuilds our master index of installed_files.
        /// Called on registry format updates, but safe to be triggered at any time.
        /// </summary>
        public void ReindexInstalled()
        {
            installed_files = new Dictionary<string, string>();

            foreach (InstalledModule module in installed_modules.Values)
            {
                foreach (string file in module.Files)
                {
                    // Register each file we know about as belonging to the given module.
                    installed_files[file] = module.identifier;
                }
            }
        }

        /// <summary>
        /// Do we what we can to repair/preen the registry.
        /// </summary>
        public void Repair()
        {
            ReindexInstalled();
        }

        #endregion

        #region Constructors

        public Registry(
            Dictionary<string, InstalledModule> installed_modules,
            Dictionary<string, AutodetectedModule> installed_preexisting_modules,
            Dictionary<string, AvailableModule> available_modules,
            Dictionary<string, string> installed_files,
            SortedDictionary<string, Repository> repositories
            )
        {
            // Is there a better way of writing constructors than this? Srsly?
            this.installed_modules = installed_modules;
            this.installed_preexisting_modules = installed_preexisting_modules;
            this.available_modules = available_modules;
            this.installed_files = installed_files;
            this.repositories = repositories;
            registry_version = LATEST_REGISTRY_VERSION;
        }

        // If deserialsing, we don't want everything put back directly,
        // thus making sure our version number is preserved, letting us
        // detect registry version upgrades.
        [JsonConstructor]
        private Registry()
        {
        }

        public static Registry Empty()
        {
            return new Registry(
                new Dictionary<string, InstalledModule>(),
                new Dictionary<string, AutodetectedModule>(),
                new Dictionary<string, AvailableModule>(),
                new Dictionary<string, string>(),
                new SortedDictionary<string, Repository>()
                );
        }

        #endregion

        #region Transaction Handling

        // We use this to record which transaction we're in.
        private string enlisted_tx;

        // This *doesn't* get called when we get enlisted in a Tx, it gets
        // called when we're about to commit a transaction. We can *probably*
        // get away with calling .Done() here and skipping the commit phase,
        // but I'm not sure if we'd get InDoubt signalling if we did that.
        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            log.Debug("Registry prepared to commit transaction");

            preparingEnlistment.Prepared();
        }

        public void InDoubt(Enlistment enlistment)
        {
            // In doubt apparently means we don't know if we've committed or not.
            // Since our TxFileMgr treats this as a rollback, so do we.
            log.Warn("Transaction involving registry in doubt.");
            Rollback(enlistment);
        }

        public void Commit(Enlistment enlistment)
        {
            // Hooray! All Tx participants have signalled they're ready.
            // So we're done, and can clear our resources.

            enlisted_tx = null;
            transaction_backup = null;

            enlistment.Done();
            log.Debug("Registry transaction committed");

            // TODO: Should we save to disk at the end of a Tx?
            // TODO: If so, we should abort if we find a save that's while a Tx is in progress?
            //
            // In either case, do we want the registry_manager to be Tx aware?
        }

        public void Rollback(Enlistment enlistment)
        {
            log.Info("Aborted transaction, rolling back in-memory registry changes.");

            // In theory, this should put everything back the way it was, overwriting whatever
            // we had previously.

            var options = new JsonSerializerSettings {ObjectCreationHandling = ObjectCreationHandling.Replace};

            JsonConvert.PopulateObject(transaction_backup, this, options);

            enlisted_tx = null;
            transaction_backup = null;

            enlistment.Done();
        }

        private void SaveState()
        {
            // Hey, you know what's a great way to back-up your own object?
            // JSON. ;)
            transaction_backup = JsonConvert.SerializeObject(this, Formatting.None);
            log.Debug("State saved");
        }

        /// <summary>
        /// "Pardon me, but I couldn't help but overhear you're in a Transaction..."
        ///
        /// Adds our registry to the current transaction. This should be called whenever we
        /// do anything which may dirty the registry.
        /// </summary>
        //
        // http://wondermark.com/1k62/
        private void SealionTransaction()
        {
            if (Transaction.Current != null)
            {
                string current_tx = Transaction.Current.TransactionInformation.LocalIdentifier;

                if (enlisted_tx == null)
                {
                    log.Debug("Pardon me, but I couldn't help overhear you're in a transaction...");
                    Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
                    SaveState();
                    enlisted_tx = current_tx;
                }
                else if (enlisted_tx != current_tx)
                {
                    log.Error("CFAN registry does not support nested transactions.");
                    throw new TransactionalKraken("CFAN registry does not support nested transactions.");
                }

                // If we're here, it's a transaction we're already participating in,
                // so do nothing.
            }
        }

        #endregion

        /// <summary>
        /// Clears all available modules from the registry.
        /// </summary>
        public void ClearAvailable()
        {
            SealionTransaction();
            available_modules = new Dictionary<string, AvailableModule>();
        }

        /// <summary>
        /// Mark a given module as available.
        /// </summary>
        public void AddAvailable(CfanModule module)
        {
            SealionTransaction();

            var identifier = module.identifier;
            // If we've never seen this module before, create an entry for it.
            if (! available_modules.ContainsKey(identifier))
            {
                log.DebugFormat("Adding new available module {0}", identifier);
                available_modules[identifier] = new AvailableModule(identifier);
            }

            // Now register the actual version that we have.
            // (It's okay to have multiple versions of the same mod.)

            log.DebugFormat("Available: {0} version {1}", identifier, module.modVersion);
            available_modules[identifier].Add(module);
        }

        /// <summary>
        /// Remove the given module from the registry of available modules.
        /// Does *nothing* if the module was not present to begin with.
        /// </summary>
        public void RemoveAvailable(string identifier, ModVersion version)
        {
            AvailableModule availableModule;
            if (available_modules.TryGetValue(identifier, out availableModule))
            {
                SealionTransaction();
                availableModule.Remove(version);
            }
        }

        /// <summary>
        /// Removes the given module from the registry of available modules.
        /// Does *nothing* if the module was not present to begin with.</summary>
        public void RemoveAvailable(CfanModule module)
        {
            RemoveAvailable(module.identifier, module.modVersion);
        }

        /// <summary>
        /// <see cref="IRegistryQuerier.Available"/>
        /// </summary>
        public List<CfanModule> Available(FactorioVersion ksp_version)
        {
            var candidates = new List<string>(available_modules.Keys);
            var compatible = new List<CfanModule>();

            // It's nice to see things in alphabetical order, so sort our keys first.
            candidates.Sort();

            //Cache
            CfanModule[] modules_for_current_version = available_modules.Values.Select(pair => pair.Latest(ksp_version)).Where(mod => mod != null).ToArray();
            // Now find what we can give our user.
            foreach (string candidate in candidates)
            {
                CfanModule available = LatestAvailable(candidate, ksp_version);

                if (available != null)
                {
                    // we need to check that we can get everything we depend on
                    bool failedDepedency = false;

                    if (available.depends != null)
                    {
                        foreach (ModDependency dependency in available.depends)
                        {
                            try
                            {
                                if (!LatestAvailableWithProvides(dependency.modName, ksp_version, modules_for_current_version).Any())
                                {
                                    log.InfoFormat("Cannot find available version with provides for {0} in registry required by {1}", dependency.modName, candidate);
                                    failedDepedency = true;
                                    break;
                                }
                            }
                            catch (KeyNotFoundException)
                            {
                                log.ErrorFormat("Cannot find available version with provides for {0} in registry", dependency.modName);
                                throw;
                            }
                            catch (ModuleNotFoundKraken)
                            {
                                log.InfoFormat("Cannot find available version with provides for {0} in registry required by {1}", dependency.modName, candidate);
                                failedDepedency = true;
                                break;
                            }
                        }
                    }

                    if (!failedDepedency)
                    {
                        compatible.Add(available);
                    }
                }
            }

            return compatible;
        }

        /// <summary>
        /// <see cref="IRegistryQuerier.Incompatible"/>
        /// </summary>
        public List<CfanModule> Incompatible(FactorioVersion ksp_version)
        {
            var candidates = new List<string>(available_modules.Keys);

            // It's nice to see things in alphabetical order, so sort our keys first.
            candidates.Sort();

            // Now find what we can give our user.

            return candidates.Select(candidate => new {candidate, available = LatestAvailable(candidate, ksp_version)})
                .Where(p => p.available == null)
                .Select(p => LatestAvailable(p.candidate, null)).ToList();
        }


        /// <summary>
        /// <see cref = "IRegistryQuerier.LatestAvailable" />
        /// </summary>

        // TODO: Consider making this internal, because practically everything should
        // be calling LatestAvailableWithProvides()
        public CfanModule LatestAvailable(
            string module,
            FactorioVersion ksp_version,
            ModDependency relationship_descriptor =null)
        {
            log.DebugFormat("Finding latest available for {0}", module);

            // TODO: Check user's stability tolerance (stable, unstable, testing, etc)

            try
            {
                return available_modules[module].Latest(ksp_version,relationship_descriptor);
            }
            catch (KeyNotFoundException)
            {
                throw new ModuleNotFoundKraken(module);
            }
        }



        /// <summary>
        /// <see cref = "IRegistryQuerier.LatestAvailableWithProvides" />
        /// </summary>
        public List<CfanModule> LatestAvailableWithProvides(string module, FactorioVersion ksp_version, ModDependency relationship_descriptor = null)
        {
            // This public interface calculates a cache of modules which
            // are compatible with the current version of KSP, and then
            // calls the private version below for heavy lifting.
            return LatestAvailableWithProvides(module, ksp_version,
                available_modules.Values.Select(pair => pair.Latest(ksp_version)).Where(mod => mod != null).ToArray(),
                relationship_descriptor);
        }

        /// <summary>
        /// Returns the latest version of a module that can be installed for
        /// the given KSP version. This is a *private* method that assumes
        /// the `available_for_current_version` list has been correctly
        /// calculated. Not for direct public consumption. ;)
        /// </summary>
        private List<CfanModule> LatestAvailableWithProvides(string module, FactorioVersion ksp_version,
            IEnumerable<CfanModule> available_for_current_version, ModDependency relationship_descriptor=null)
        {
            log.DebugFormat("Finding latest available with provides for {0}", module);

            // TODO: Check user's stability tolerance (stable, unstable, testing, etc)

            var modules = new List<CfanModule>();

            try
            {
                // If we can find the module requested for our KSP, use that.
                CfanModule mod = LatestAvailable(module, ksp_version, relationship_descriptor);
                if (mod != null)
                {
                    modules.Add(mod);
                }
            }
            catch (ModuleNotFoundKraken)
            {
                // It's cool if we can't find it, though.
            }

            // Walk through all our available modules, and see if anything
            // provides what we need.

            // Get our candidate module. We can assume this is non-null, as
            // if it *is* null then available_for_current_version is corrupted,
            // and something is terribly wrong.
            foreach (CfanModule candidate in available_for_current_version)
            {
                // Find everything this module provides (for our version of KSP)
                List<string> provides = candidate.providesNames.ToList();

                // If the module has provides, and any of them are what we're looking
                // for, the add it to our list.
                if (provides != null && provides.Any(provided => provided == module))
                {
                    modules.Add(candidate);
                }
            }
            return modules;
        }

        /// <summary>
        /// Returns the specified CkanModule with the version specified,
        /// or null if it does not exist.
        /// <see cref = "IRegistryQuerier.GetModuleByVersion" />
        /// </summary>
        public CfanModule GetModuleByVersion(string ident, AbstractVersion version)
        {
            log.DebugFormat("Trying to find {0} version {1}", ident, version);

            if (!available_modules.ContainsKey(ident))
            {
                return null;
            }

            AvailableModule available = available_modules[ident];
            return available.ByVersion(new ModVersion(version.ToString()));
        }

        /// <summary>
        ///     Register the supplied module as having been installed, thereby keeping
        ///     track of its metadata and files.
        /// </summary>
        public void RegisterModule(CfanModule mod, IEnumerable<string> absolute_files, KSP ksp)
        {
            SealionTransaction();

            // But we also want to keep track of all its files.
            // We start by checking to see if any files are owned by another mod,
            // if so, we abort with a list of errors.

            var inconsistencies = new List<string>();

            // We always work with relative files, so let's get some!
            IEnumerable<string> relative_files = absolute_files.Select(x => ksp.ToRelativeGameDataDir(x));

            // For now, it's always cool if a module wants to register a directory.
            // We have to flip back to absolute paths to actually test this.
            foreach (string file in relative_files.Where(file => !Directory.Exists(ksp.ToAbsoluteGameDataDir(file))))
            {
                string owner;
                if (installed_files.TryGetValue(file, out owner))
                {
                    // Woah! Registering an already owned file? Not cool!
                    // (Although if it existed, we should have thrown a kraken well before this.)
                    inconsistencies.Add(
                        string.Format("{0} wishes to install {1}, but this file is registered to {2}",
                            mod.identifier, file, owner
                            ));
                }
            }

            if (inconsistencies.Count > 0)
            {
                throw new InconsistentKraken(inconsistencies);
            }

            // If everything is fine, then we copy our files across. By not doing this
            // in the loop above, we make sure we don't have a half-registered module
            // when we throw our exceptinon.

            // This *will* result in us overwriting who owns a directory, and that's cool,
            // directories aren't really owned like files are. However because each mod maintains
            // its own list of files, we'll remove directories when the last mod using them
            // is uninstalled.
            foreach (string file in relative_files)
            {
                installed_files[file] = mod.identifier;
            }

            // Finally, register our module proper.
            var installed = new InstalledModule(ksp, mod, relative_files);
            installed_modules.Add(mod.identifier, installed);
        }

        /// <summary>
        /// Deregister a module, which must already have its files removed, thereby
        /// forgetting abouts its metadata and files.
        ///
        /// Throws an InconsistentKraken if not all files have been removed.
        /// </summary>
        public void DeregisterModule(KSP ksp, string module)
        {
            SealionTransaction();

            var inconsistencies = new List<string>();

            var absolute_files = installed_modules[module].Files.Select(ksp.ToAbsoluteGameDataDir);
            // Note, this checks to see if a *file* exists; it doesn't
            // trigger on directories, which we allow to still be present
            // (they may be shared by multiple mods.

            foreach (var absolute_file in absolute_files.Where(File.Exists))
            {
                inconsistencies.Add(string.Format(
                    "{0} is registered to {1} but has not been removed!",
                    absolute_file, module));
            }

            if (inconsistencies.Count > 0)
            {
                // Uh oh, what mess have we got ourselves into now, Inconsistency Kraken?
                throw new InconsistentKraken(inconsistencies);
            }

            // Okay, all the files are gone. Let's clear our metadata.
            foreach (string rel_file in installed_modules[module].Files)
            {
                installed_files.Remove(rel_file);
            }

            // Bye bye, module, it's been nice having you visit.
            installed_modules.Remove(module);
        }

        /// <summary>
        /// Registers the given DLL as having been installed. This provides some support
        /// for pre-CKAN modules.
        ///
        /// Does nothing if the DLL is already part of an installed module.
        /// </summary>
        public void RegisterPreexistingModule(KSP ksp, string absolute_path, ModInfoJson modInfo)
        {
            SealionTransaction();

            string relative_path = ksp.ToRelativeGameDataDir(absolute_path);

            InstalledModule owner;
            if (installed_modules.TryGetValue(modInfo.name, out owner))
            {
                log.InfoFormat(
                    "Not registering {0}, it is already installed",
                    relative_path
                );
                return;
            }

            log.InfoFormat("Registering {0} from {1}", modInfo.name, relative_path);

            // We're fine if we overwrite an existing key.
            installed_preexisting_modules[modInfo.name] = new AutodetectedModule(relative_path, modInfo);
        }

        /// <summary>
        /// Clears knowledge of all DLLs from the registry.
        /// </summary>
        public void ClearPreexistingModules()
        {
            SealionTransaction();
            installed_preexisting_modules = new Dictionary<string, AutodetectedModule>();
        }

        /// <summary>
        /// <see cref = "IRegistryQuerier.Installed" />
        /// </summary>
        public Dictionary<string, AbstractVersion> Installed(bool withProvides = true)
        {
            var installed = new Dictionary<string, AbstractVersion>();

            // Index our Preexisting Modules, because we like them as much as any other mod
            // unlike CKAN staff who hates their dlls!
            foreach (var preexisting in installed_preexisting_modules)
            {
                AbstractVersion modVersion = preexisting.Value.modInfo.version;
                if (modVersion != null)
                {
                    installed[preexisting.Key] = modVersion;
                }
            }

            // Index our provides list, so users can see virtual packages
            if (withProvides)
            {
                foreach (var provided in Provided())
                {
                    installed[provided.Key] = provided.Value;
                }
            }

            // Index our installed modules (which may overwrite the installed DLLs and provides)
            foreach (var modinfo in installed_modules)
            {
                installed[modinfo.Key] = modinfo.Value.Module.modVersion;
            }

            return installed;
        }

        /// <summary>
        /// <see cref = "IRegistryQuerier.InstalledModule" />
        /// </summary>
        public InstalledModule InstalledModule(string module)
        {
            // In theory, someone could then modify the data they get back from
            // this, so we sea-lion just in case.

            SealionTransaction();

            InstalledModule installedModule;
            return installed_modules.TryGetValue(module, out installedModule) ? installedModule : null;
        }

        /// <summary>
        /// Returns a dictionary of provided (virtual) modules, and a
        /// ProvidesVersion indicating what provides them.
        /// </summary>

        // TODO: In the future it would be nice to cache this list, and mark it for rebuild
        // if our installed modules change.
        internal Dictionary<string, ProvidedVersion> Provided()
        {
            Dictionary<string, ProvidedVersion> installed = new Dictionary<string, ProvidedVersion>();
            // identifierProvided -> ProvidedVersion (identifierProvidedBy, providedVersionString)
            return installed;
        }

        /// <summary>
        /// <see cref = "IRegistryQuerier.InstalledVersion" />
        /// </summary>
        public AbstractVersion InstalledVersion(string modIdentifier, bool with_provides=true)
        {
            InstalledModule installedModule;

            // If it's genuinely installed, return the details we have.
            if (installed_modules.TryGetValue(modIdentifier, out installedModule))
            {
                return installedModule.Module.modVersion;
            }

            // If it's in our autodetected registry, return that.
            if (installed_preexisting_modules.ContainsKey(modIdentifier))
            {
                return new AutodetectedVersion(installed_preexisting_modules[modIdentifier].modInfo.version.ToString());
            }

            // Finally we have our provided checks. We'll skip these if
            // withProvides is false.
            if (!with_provides) return null;

            var provided = Provided();

            ProvidedVersion version;
            return provided.TryGetValue(modIdentifier, out version) ? version : null;
        }

        /// <summary>
        /// <see cref = "IRegistryQuerier.GetInstalledVersion" />
        /// </summary>
        public CfanModule GetInstalledVersion(string mod_identifer)
        {
            InstalledModule installedModule;
            return installed_modules.TryGetValue(mod_identifer, out installedModule) ? installedModule.Module : null;
        }

        /// <summary>
        /// Returns the module which owns this file, or null if not known.
        /// Throws a PathErrorKraken if an absolute path is provided.
        /// </summary>
        public string FileOwner(string file)
        {
            file = KSPPathUtils.NormalizePath(file);

            if (Path.IsPathRooted(file))
            {
                throw new PathErrorKraken(
                    file,
                    "KSPUtils.FileOwner can only work with relative paths."
                );
            }

            string fileOwner;
            return installed_files.TryGetValue(file, out fileOwner) ? fileOwner : null;
        }

        /// <summary>
        /// <see cref="IRegistryQuerier.CheckSanity"/>
        /// </summary>
        public void CheckSanity()
        {
            IEnumerable<CfanModule> installed = from pair in installed_modules select pair.Value.Module;
            SanityChecker.EnforceConsistency(installed, installed_preexisting_modules.Keys);
        }

        /// <summary>
        /// Finds and returns all modules that could not exist without the listed modules installed, including themselves.
        /// Acts recursively.
        /// </summary>
        internal static HashSet<string> FindReverseDependencies(IEnumerable<string> modules_to_remove, IEnumerable<CfanModule> orig_installed, IEnumerable<string> dlls)
        {
            while (true)
            {
                // Make our hypothetical install, and remove the listed modules from it.
                HashSet<CfanModule> hypothetical = new HashSet<CfanModule>(orig_installed); // Clone because we alter hypothetical.
                hypothetical.RemoveWhere(mod => modules_to_remove.Contains(mod.identifier) || modules_to_remove.Contains(mod.ToString()));

                log.DebugFormat("Started with {0}, removing {1}, and keeping {2}; our dlls are {3}", string.Join(", ", orig_installed), string.Join(", ", modules_to_remove), string.Join(", ", hypothetical), string.Join(", ", dlls));

                // Find what would break with this configuration.
                // The Values.SelectMany() flattens our list of broken mods.
                var broken = new HashSet<string>(SanityChecker.FindUnmetDependencies(hypothetical, dlls)
                    .Values.SelectMany(x => x).Select(x => x.identifier));

                // If nothing else would break, it's just the list of modules we're removing.
                HashSet<string> to_remove = new HashSet<string>(modules_to_remove);

                if (to_remove.IsSupersetOf(broken))
                {
                    log.DebugFormat("{0} is a superset of {1}, work done", string.Join(", ", to_remove), string.Join(", ", broken));
                    return to_remove;
                }

                // Otherwise, remove our broken modules as well, and recurse.
                broken.UnionWith(to_remove);
                modules_to_remove = broken;
            }
        }

        /// <summary>
        /// Return modules which are dependent on the modules passed in or modules in the return list
        /// </summary>
        public HashSet<string> FindReverseDependencies(IEnumerable<string> modules_to_remove)
        {
            var installed = new HashSet<CfanModule>(installed_modules.Values.Select(x => x.Module));
            return FindReverseDependencies(modules_to_remove, installed, new HashSet<string>(installed_preexisting_modules.Keys));
        }
    }
}
