using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CKAN.Factorio;

namespace CKAN.CmdLine
{
    public class Update : ICommand
    {
        public IUser user { get; set; }

        public Update(IUser user)
        {
            this.user = user;
        }

        public int RunCommand(CKAN.KSP ksp, object raw_options)
        {
            UpdateOptions options = (UpdateOptions) raw_options;

            List<CfanModule> available_prior = null;

            user.RaiseMessage("Downloading updates...");

            if (options.list_changes)
            {
                // Get a list of available modules prior to the update.
                available_prior = ksp.Registry.Available(ksp.Version());
            }

            // If no repository is selected, select all.
            if (options.repo == null)
            {
                options.update_all = true;
            }

            try
            {
                if (options.update_all)
                {
                    UpdateRepository(ksp);
                }
                else
                {
                    UpdateRepository(ksp, options.repo);
                }
            }
            catch (MissingCertificateKraken kraken)
            {
                // Handling the kraken means we have prettier output.
                user.RaiseMessage(kraken.ToString());
                return Exit.ERROR;
            }

            if (options.list_changes)
            {
                PrintChanges(available_prior, ksp.Registry.Available(ksp.Version()));
            }

            return Exit.OK;
        }

        /// <summary>
        /// Locates the changes between the prior and post state of the modules..
        /// </summary>
        /// <param name="modules_prior">List of the available modules prior to the update.</param>
        /// <param name="modules_post">List of the available modules after the update.</param>
        private void PrintChanges(List<CfanModule> modules_prior, List<CfanModule> modules_post)
        {
            var prior = new HashSet<CfanModule>(modules_prior, new CfanModule.NameComparer());
            var post = new HashSet<CfanModule>(modules_post, new CfanModule.NameComparer());


            var added = new HashSet<CfanModule>(post.Except(prior, new CfanModule.NameComparer()));
            var removed = new HashSet<CfanModule>(prior.Except(post, new CfanModule.NameComparer()));


            var unchanged = post.Intersect(prior);//Default compare includes versions
            var updated = post.Except(unchanged).Except(added).Except(removed).ToList();

            // Print the changes.
            user.RaiseMessage("Found {0} new modules, {1} removed modules and {2} updated modules.", added.Count(), removed.Count(), updated.Count());

            if (added.Count > 0)
            {
                PrintModules("New modules [Name (CFAN identifier)]:", added);
            }

            if (removed.Count > 0)
            {
                PrintModules("Removed modules [Name (CFAN identifier)]:", removed);
            }

            if (updated.Count > 0)
            {
                PrintModules("Updated modules [Name (CFAN identifier)]:", updated);
            }
        }

        /// <summary>
        /// Prints a message and a list of modules. Ends with a blank line.
        /// </summary>
        /// <param name="message">The message to print.</param>
        /// <param name="modules">The modules to list.</param>
        private void PrintModules(string message, IEnumerable<CfanModule> modules)
        {
            // Check input.
            if (message == null)
            {
                throw new BadCommandKraken("Message cannot be null.");
            }

            if (modules == null)
            {
                throw new BadCommandKraken("List of modules cannot be null.");
            }

            user.RaiseMessage(message);

            foreach(CfanModule module in modules)
            {
                user.RaiseMessage("{0} ({1})", module.title, module.identifier);
            }

            user.RaiseMessage("");
        }

        /// <summary>
        /// Updates the repository.
        /// </summary>
        /// <param name="ksp">The KSP instance to work on.</param>
        /// <param name="repository">Repository to update. If null all repositories are used.</param>
        private void UpdateRepository(CKAN.KSP ksp, string repository = null)
        {
            RegistryManager registry_manager = RegistryManager.Instance(ksp);

            var updated = repository == null
                ? CKAN.Repo.UpdateAllRepositories(registry_manager, ksp, user)
                : CKAN.Repo.Update(registry_manager, ksp, user, true, repository);

            user.RaiseMessage("Updated information on {0} available modules", updated);
        }
    }
}
