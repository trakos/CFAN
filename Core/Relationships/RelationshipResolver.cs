using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CKAN.Factorio;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Version;
using log4net;

namespace CKAN
{

    // TODO: It would be lovely to get rid of the `without` fields,
    // and replace them with `with` fields. Humans suck at inverting
    // cases in their heads.
    public class RelationshipResolverOptions : ICloneable
    {
        /// <summary>
        /// If true, add recommended mods, and their recommendations.
        /// </summary>
        public bool with_recommends = true;

        /// <summary>
        /// If true, add suggests, but not suggested suggests. :)
        /// </summary>
        public bool with_suggests = false;

        /// <summary>
        /// If true, add suggested modules, and *their* suggested modules, too!
        /// </summary>
        public bool with_all_suggests = false;

        /// <summary>
        /// If true, surpresses the TooManyProvides kraken when resolving
        /// relationships. Otherwise, we just pick the first.
        /// </summary>
        public bool without_toomanyprovides_kraken = false;

        /// <summary>
        /// If true, we skip our sanity check at the end of our relationship
        /// resolution. Note that non-sane resolutions can't actually be
        /// installed, so this is mostly useful for giving the user feedback
        /// on failed resolutions.
        /// </summary>
        public bool without_enforce_consistency = false;

        /// <summary>
        /// If true, we'll populate the `conflicts` field, rather than immediately
        /// throwing a kraken when inconsistencies are detected. Again, these
        /// solutions are non-installable, so mostly of use to provide user
        /// feedback when things go wrong.
        /// </summary>
        public bool procede_with_inconsistencies = false;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    // TODO: RR currently conducts a depth-first resolution of requirements. While we do the
    // right thing in processing all dependencies first, then recommends, and then suggests,
    // we could find that a recommendation many layers deep prevents a recommendation in the
    // original mod's recommends list.
    //
    // If we resolved in things breadth-first order, we're less likely to encounter surprises
    // where a nth-deep recommend blocks a top-level recommend.

    // TODO: Add mechanism so that clients can add mods with relationshup other than UserAdded.
    // Currently only made to support the with_{} options.


    /// <summary>
    /// A class used to resolve relationships between mods. Primarily used to satisfy missing dependencies and to check for conflicts on proposed installs.
    /// </summary>
    /// <remarks>
    /// All constructors start with currently installed modules, to remove <see cref="RelationshipResolver.RemoveModsFromInstalledList" />
    /// </remarks>
    public class RelationshipResolver
    {
        // A list of all the mods we're going to install.
        private static readonly ILog log = LogManager.GetLogger(typeof (RelationshipResolver));
        private readonly Dictionary<string, CfanModule> modlist = new Dictionary<string, CfanModule>();
        private readonly List<CfanModule> user_requested_mods = new List<CfanModule>();

        //TODO As the conflict detection gets more advanced there is a greater need to have messages in here
        // as recreating them from reasons is no longer possible.
        private readonly List<KeyValuePair<CfanModule, CfanModule>> conflicts =
            new List<KeyValuePair<CfanModule, CfanModule>>();
        private readonly Dictionary<CfanModule, SelectionReason> reasons =
            new Dictionary<CfanModule, SelectionReason>(new CfanModule.NameComparer());

        private readonly IRegistryQuerier registry;
        private readonly FactorioVersion kspversion;
        private readonly RelationshipResolverOptions options;
        private readonly HashSet<CfanModule> installed_modules;

        /// <summary>
        /// Creates a new Relationship resolver.
        /// </summary>
        /// <param name="options"><see cref="RelationshipResolverOptions"/></param>
        /// <param name="registry">The registry to use</param>
        /// <param name="kspversion">The version of the install that the registry corresponds to</param>
        public RelationshipResolver(RelationshipResolverOptions options, IRegistryQuerier registry, FactorioVersion kspversion)
        {
            this.registry = registry;
            this.kspversion = kspversion;
            this.options = options;

            installed_modules = new HashSet<CfanModule>(registry.InstalledModules.Select(i_module => i_module.Module));
            var installed_relationship = new SelectionReason.Installed();
            foreach (var module in installed_modules)
            {
                reasons.Add(module, installed_relationship);
            }
        }

        private static List<CfanModule> getModulesFromRegistry(
            IEnumerable<CfanModuleIdAndVersion> module_names, IRegistryQuerier registry, FactorioVersion kspversion)
        {
            return module_names.Select(
                    p =>
                    {
                        var mod = p.version != null
                            ? registry.GetModuleByVersion(p.identifier, p.version)
                            : registry.LatestAvailable(p.identifier, kspversion);
                        if (mod == null)
                        {
                            throw new ModuleNotFoundKraken(p.identifier, p.version?.ToString(), "Module not found.");
                        }
                        return mod;
                    }).ToList();
        }

        /// <summary>
        /// Attempts to convert the module_names to ckan modules via  CkanModule.FromIDandVersion and then calls RelationshipResolver.ctor(IEnumerable{CkanModule}, Registry, KSPVersion)"/>
        /// </summary>
        /// <param name="module_names"></param>
        /// <param name="options"></param>
        /// <param name="registry"></param>
        /// <param name="kspversion"></param>
        public RelationshipResolver(IEnumerable<CfanModuleIdAndVersion> module_names, RelationshipResolverOptions options, IRegistryQuerier registry, FactorioVersion kspversion) :
                this(getModulesFromRegistry(module_names, registry, kspversion),
                    options,
                    registry,
                    kspversion)
        {
            // Does nothing, just calls the other overloaded constructor
        }

        /// <summary>
        /// Creates a new resolver that will find a way to install all the modules specified.
        /// </summary>
        public RelationshipResolver(ICollection<CfanModule> modules, RelationshipResolverOptions options, IRegistryQuerier registry,
            FactorioVersion kspversion):this(options,registry,kspversion)
        {
            AddModulesToInstall(modules.ToArray());
        }

        /// <summary>
        ///     Returns the default options for relationship resolution.
        /// </summary>

        // TODO: This should just be able to return a new RelationshipResolverOptions
        // and the defaults in the class definition should do the right thing.
        public static RelationshipResolverOptions DefaultOpts()
        {
            var opts = new RelationshipResolverOptions
            {
                with_recommends = true,
                with_suggests = false,
                with_all_suggests = false
            };

            return opts;
        }

        /// <summary>
        /// Add modules to consideration of the relationship resolver.
        /// </summary>
        /// <param name="ckan_modules">Modules to attempt to install</param>
        public void AddModulesToInstall(ICollection<CfanModule> ckan_modules)
        {
            log.DebugFormat("Processing relationships for {0} modules", ckan_modules.Count());

            // Start by figuring out what versions we're installing, and then
            // adding them to the list. This *must* be pre-populated with all
            // user-specified modules, as they may be supplying things that provide
            // virtual packages.
            foreach (var module in ckan_modules)
            {
                log?.DebugFormat("Preparing to resolve relationships for {0} {1}", module.identifier, module.modVersion);

                //Need to check against installed mods and those to install.
                var mods = modlist.Values.Concat(installed_modules).Where(listed_mod => listed_mod.ConflictsWith(module));
                foreach (var listed_mod in mods)
                {
                    if (options.procede_with_inconsistencies)
                    {
                        conflicts.Add(new KeyValuePair<CfanModule, CfanModule>(listed_mod, module));
                        conflicts.Add(new KeyValuePair<CfanModule, CfanModule>(module, listed_mod));
                    }
                    else
                    {
                        throw new InconsistentKraken(string.Format("{0} conflicts with {1}, can't install both.", module,
                            listed_mod));
                    }
                }

                user_requested_mods.Add(module);
                Add(module, new SelectionReason.UserRequested());
            }

            // Now that we've already pre-populated the modlist, we can resolve
            // the rest of our dependencies.

            foreach (var module in user_requested_mods)
            {
                log.InfoFormat("Resolving relationships for {0}", module.identifier);
                Resolve(module, options);
            }

            if (!options.without_enforce_consistency)
            {
                var final_modules = new List<CfanModule>(modlist.Values);
                final_modules.AddRange(installed_modules);
                // Finally, let's do a sanity check that our solution is actually sane.
                SanityChecker.EnforceConsistency(
                    final_modules,
                    registry.InstalledPreexistingModules
                    );
            }
        }

        /// <summary>
        /// Removes mods from the list of installed modules. Intended to be used for cases
        /// in which the mod is to be un-installed.
        /// </summary>
        /// <param name="mods">The mods to remove.</param>
        public void RemoveModsFromInstalledList(IEnumerable<CfanModule> mods)
        {
            foreach (var module in mods)
            {
                installed_modules.Remove(module);
                conflicts.RemoveAll(kvp => kvp.Key.Equals(module) || kvp.Value.Equals(module));
            }
        }

        /// <summary>
        /// Resolves all relationships for a module.
        /// May recurse to ResolveStanza, which may add additional modules to be installed.
        /// </summary>
        private void Resolve(CfanModule module, RelationshipResolverOptions options, IEnumerable<ModDependency> old_stanza = null)
        {
            // Even though we may resolve top-level suggests for our module,
            // we don't install suggestions all the down unless with_all_suggests
            // is true.
            var sub_options = (RelationshipResolverOptions) options.Clone();
            sub_options.with_suggests = false;

            log.InfoFormat("Resolving dependencies for {0}", module.identifier);
            ResolveStanza(module.depends, new SelectionReason.Depends(module), sub_options, false, old_stanza);

            if (options.with_recommends)
            {
                log.DebugFormat("Resolving recommends for {0}", module.identifier);
                ResolveStanza(module.recommends, new SelectionReason.Recommended(module), sub_options, true, old_stanza);
            }

            if (options.with_suggests || options.with_all_suggests)
            {
                log.DebugFormat("Resolving suggests for {0}", module.identifier);
                ResolveStanza(module.suggests, new SelectionReason.Suggested(module), sub_options, true, old_stanza);
            }
        }

        /// <summary>
        /// Resolve a relationship stanza (a list of relationships).
        /// This will add modules to be installed, if required.
        /// May recurse back to Resolve for those new modules.
        ///
        /// If `soft_resolve` is true, we warn rather than throw exceptions on mods we cannot find.
        /// If `soft_resolve` is false (default), we throw a ModuleNotFoundKraken if we can't find a dependency.
        ///
        /// Throws a TooManyModsProvideKraken if we have too many choices and
        /// options.without_toomanyprovides_kraken is not set.
        ///
        /// See RelationshipResolverOptions for further adjustments that can be made.
        ///
        /// </summary>

        private void ResolveStanza(IEnumerable<ModDependency> stanza, SelectionReason reason,
            RelationshipResolverOptions options, bool soft_resolve = false, IEnumerable<ModDependency> old_stanza = null)
        {
            if (stanza == null)
            {
                return;
            }

            foreach (ModDependency descriptor in stanza)
            {
                string dep_name = descriptor.modName;
                log.DebugFormat("Considering {0}", dep_name);

                // If we already have this dependency covered, skip.
                // If it's already installed, skip.

                if (modlist.ContainsKey(dep_name))
                {
                    var module = modlist[dep_name];
                    
                    if (descriptor.isSatisfiedBy(module.identifier, module.modVersion))
                        continue;
                    //TODO Ideally we could check here if it can be replaced by the version we want.
                    if (options.procede_with_inconsistencies)
                    {
                        conflicts.Add(new KeyValuePair<CfanModule, CfanModule>(module,reason.Parent));
                        conflicts.Add(new KeyValuePair<CfanModule, CfanModule>(reason.Parent,module));
                        continue;
                    }
                    throw new InconsistentKraken(
                        string.Format(
                            "{0} requires a version {1}. However a incompatible version, {2}, is in the resolver",
                            dep_name, descriptor.minVersion, module.modVersion));
                }

                if (registry.IsInstalled(dep_name))
                {
                    if(descriptor.isSatisfiedBy(dep_name, registry.InstalledVersion(dep_name)))
                        continue;
                    var module = registry.InstalledModule(dep_name).Module;

                    //TODO Ideally we could check here if it can be replaced by the version we want.
                    if (options.procede_with_inconsistencies)
                    {
                        conflicts.Add(new KeyValuePair<CfanModule, CfanModule>(module, reason.Parent));
                        conflicts.Add(new KeyValuePair<CfanModule, CfanModule>(reason.Parent, module));
                        continue;
                    }
                    throw new InconsistentKraken(
                        string.Format(
                            "{0} requires a version {1}. However a incompatible version, {2}, is already installed",
                            dep_name, descriptor.minVersion, registry.InstalledVersion(dep_name)));
                }

                var descriptor1 = descriptor;
                List<CfanModule> candidates = registry.LatestAvailableWithProvides(dep_name, kspversion, descriptor)
                    .Where(mod => descriptor1.isSatisfiedBy(mod.identifier, mod.modVersion) && MightBeInstallable(mod))
                    .ToList();

                if (candidates.Count == 0)
                {
                    if (!soft_resolve)
                    {
                        log.ErrorFormat("Dependency on {0} found, but nothing provides it.", dep_name);
                        throw new ModuleNotFoundKraken(dep_name);
                    }
                    log.InfoFormat("{0} is recommended/suggested, but nothing provides it.", dep_name);
                    continue;
                }
                if (candidates.Count > 1)
                {
                    // Oh no, too many to pick from!
                    // TODO: It would be great if instead we picked the one with the
                    // most recommendations.
                    if (options.without_toomanyprovides_kraken)
                    {
                        continue;
                    }

                    // If we've got a parent stanza that has a relationship on a mod that provides what
                    // we need, then select that.
                    if (old_stanza != null)
                    {
                        List<CfanModule> provide = candidates.Where(can => old_stanza.Where(relation => can.identifier == relation.modName).Any()).ToList();
                        if (!provide.Any() || provide.Count() > 1)
                        {
                            //We still have either nothing, or too my to pick from
                            //Just throw the TMP now
                            throw new TooManyModsProvideKraken(dep_name, candidates);
                        }
                        candidates[0] = provide.First();
                    }
                    else
                    {
                        throw new TooManyModsProvideKraken(dep_name, candidates);
                    }
                }

                CfanModule candidate = candidates[0];

                // Finally, check our candidate against everything which might object
                // to it being installed; that's all the mods which are fixed in our
                // list thus far, as well as everything on the system.

                var fixed_mods = new HashSet<CfanModule>(modlist.Values);
                fixed_mods.UnionWith(installed_modules);

                var conflicting_mod = fixed_mods.FirstOrDefault(mod => mod.ConflictsWith(candidate));
                if (conflicting_mod == null)
                {
                    // Okay, looks like we want this one. Adding.
                    Add(candidate, reason);
                    Resolve(candidate, options, stanza);
                }
                else if (soft_resolve)
                {
                    log.InfoFormat("{0} would cause conflicts, excluding it from consideration", candidate);
                }
                else
                {
                    if (options.procede_with_inconsistencies)
                    {
                        Add(candidate, reason);
                        conflicts.Add(new KeyValuePair<CfanModule, CfanModule>(conflicting_mod, candidate));
                        conflicts.Add(new KeyValuePair<CfanModule, CfanModule>(candidate, conflicting_mod));
                    }
                    else
                    {
                        throw new InconsistentKraken(string.Format("{0} conflicts with {1}, can't install both.", conflicting_mod,
                            candidate));
                    }
                }
            }
        }

        /// <summary>
        /// Adds the specified module to the list of modules we're installing.
        /// This also adds its provides list to what we have available.
        /// </summary>
        private void Add(CfanModule module, SelectionReason reason)
        {
            if (module.isMetapackage)
                return;

            log.DebugFormat("Adding {0} {1}", module.identifier, module.modVersion);

            if (modlist.ContainsKey(module.identifier))
            {
                // We should never be adding something twice!
                log.ErrorFormat("Assertion failed: Adding {0} twice in relationship resolution", module.identifier);
                throw new ArgumentException("Already contains module:" + module.identifier);
            }
            modlist.Add(module.identifier, module);
            if(!reasons.ContainsKey(module)) reasons.Add(module, reason);

            log.DebugFormat("Added {0}", module.identifier);
            // Stop here if it doesn't have any provide aliases.
            if (module.providesNames == null)
            {
                return;
            }

            // Handle provides/aliases if it does.

            // It's okay if there's already a key for one of our aliases
            // in the resolution list. In which case, we don't do anything.
            var aliases = module.providesNames.Where(alias => !modlist.ContainsKey(alias));
            foreach (string alias in aliases)
            {
                log.DebugFormat("Adding {0} providing {1}", module.identifier, alias);
                modlist.Add(alias, module);
            }
        }

        /// <summary>
        /// Tests that a module might be able to be installed via checking if dependencies
        /// exist for current version.
        /// </summary>
        /// <param name="module">The module to consider</param>
        /// <param name="compatible">For internal use</param>
        /// <returns>If it has dependencies compatible for the current version</returns>
        private bool MightBeInstallable(CfanModule module, List<string> compatible = null)
        {
            if (module.depends == null) return true;
            if (compatible == null)
            {
                compatible = new List<string>();
            }
            else if (compatible.Contains(module.identifier))
            {
                return true;
            }
            //When checking the dependencies we assume that this module is installable
            // in case a dependent depends on it
            compatible.Add(module.identifier);

            var needed = module.depends.Select(depend => registry.LatestAvailableWithProvides(depend.modName, kspversion));
            //We need every dependency to have at least one possible module
            var installable = needed.All(need => need.Any(mod => MightBeInstallable(mod, compatible)));
            compatible.Remove(module.identifier);
            return installable;
        }


        /// <summary>
        /// Returns a list of all modules to install to satisfy the changes required.
        /// </summary>
        public List<CfanModule> ModList()
        {
            var modules = new HashSet<CfanModule>(modlist.Values);
            return modules.ToList();
        }

        /// <summary>
        ///  Returns a IList consisting of keyValuePairs containing conflicting mods.
        /// Note: (a,b) in the list should imply that (b,a) is in the list.
        /// </summary>
        public Dictionary<CfanModule, String> ConflictList
        {
            get
            {
                var dict = new Dictionary<CfanModule, String>();
                foreach (var conflict in conflicts)
                {
                    var module = conflict.Key;
                    dict[module] = string.Format("{0} conflicts with {1}\n\n{0}:\n{2}\n{1}:\n{3}",
                        module.identifier, conflict.Value.identifier,
                        ReasonStringFor(module), ReasonStringFor(conflict.Value));
                    ;
                }
                return dict;
            }
        }

        public bool IsConsistent
        {
            get { return !conflicts.Any(); }
        }

        /// <summary>
        /// Displays a user readable string explaining why the mod was chosen.
        /// </summary>
        /// <param name="mod">A Mod in the resolvers modlist. Must not be null</param>
        /// <returns></returns>
        public string ReasonStringFor(CfanModule mod)
        {
            var reason = ReasonFor(mod);
            var is_root_type = reason.GetType() == typeof (SelectionReason.UserRequested)
                || reason.GetType() == typeof(SelectionReason.Installed);
            return is_root_type
                ? reason.Reason
                : reason.Reason + ReasonStringFor(reason.Parent);
        }

        public SelectionReason ReasonFor(CfanModule mod)
        {
            if (mod == null) throw new ArgumentNullException();
            if (!ModList().Contains(mod))
            {
                throw new ArgumentException("Mod " + mod.identifier + " is not in the list");
            }

            return reasons[mod];
        }
    }

    /// <summary>
    /// Used to keep track of the relationships between modules in the resolver.
    /// Intended to be used for displaying messages to the user.
    /// </summary>
    public abstract class SelectionReason
    {
        //Currently assumed to exist for any relationship other than useradded or installed
        public virtual CfanModule Parent { get; protected set; }
        //Should contain a newline at the end of the string.
        public abstract String Reason { get; }


        public class Installed : SelectionReason
        {
            public override CfanModule Parent
            {
                get
                {
                    Debug.Assert(false);
                    throw new Exception("Should never be called on Installed");
                }
            }

            public override string Reason => "  Currently installed.\n";
        }

        public class UserRequested : SelectionReason
        {
            public override CfanModule Parent
            {
                get
                {
                    Debug.Assert(false);
                    throw new Exception("Should never be called on UserRequested");
                }
            }

            public override string Reason => "  Requested by user.\n";
        }

        public sealed class Suggested : SelectionReason
        {
            public Suggested(CfanModule module)
            {
                if (module == null) throw new ArgumentNullException();
                Parent = module;
            }

            public override string Reason => "  Suggested by " + Parent.identifier + ".\n";
        }

        public sealed class Depends : SelectionReason
        {
            public Depends(CfanModule module)
            {
                if (module == null) throw new ArgumentNullException();
                Parent = module;
            }

            public override string Reason => "  To satisfy dependency from " + Parent.identifier + ".\n";
        }

        public sealed class Recommended : SelectionReason
        {
            public Recommended(CfanModule module)
            {
                if (module == null) throw new ArgumentNullException();
                Parent = module;
            }

            public override string Reason => "  Recommended by " + Parent.identifier + ".\n";
        }
    }
}
