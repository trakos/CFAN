using System.Collections.Generic;
using CommandLine;

namespace CKAN.CmdLine
{
    // Look, parsing options is so easy and beautiful I made
    // it into a special class for you to admire!

    public class Options
    {
        public string action { get; set; }
        public object options { get; set; }

        /// <summary>
        /// Returns an options object on success. Prints a default help
        /// screen and throws a BadCommandKraken on failure.
        /// </summary>
        public Options(string[] args)
        {
            Parser.Default.ParseArgumentsStrict
            (
                args, new Actions(), (verb, suboptions) =>
                {
                    action = verb;
                    options = suboptions;
                },
                delegate
                {
                    throw (new BadCommandKraken());
                }
            );
        }
    }

    // Actions supported by our client go here.
    // TODO: Figure out how to do per action help screens.

    internal class Actions
    {
        [VerbOption("gui", HelpText = "Start the CFAN GUI")]
        public GuiOptions GuiOptions { get; set; }

        [VerbOption("search", HelpText = "Search for mods")]
        public SearchOptions SearchOptions { get; set; }

        [VerbOption("upgrade", HelpText = "Upgrade an installed mod")]
        public UpgradeOptions Upgrade { get; set; }

        [VerbOption("update", HelpText = "Update list of available mods")]
        public UpdateOptions Update { get; set; }

        [VerbOption("available", HelpText = "List available mods")]
        public AvailableOptions Available { get; set; }

        [VerbOption("install", HelpText = "Install a Factorio mod")]
        public InstallOptions Install { get; set; }

        [VerbOption("remove", HelpText = "Remove an installed mod")]
        public RemoveOptions Remove { get; set; }

        [VerbOption("scan", HelpText = "Scan for manually installed Factorio mods")]
        public ScanOptions Scan { get; set; }

        [VerbOption("list", HelpText = "List installed modules")]
        public ListOptions List { get; set; }

        [VerbOption("show", HelpText = "Show information about a mod")]
        public ShowOptions Show { get; set; }

        [VerbOption("clean", HelpText = "Clean away downloaded files from the cache")]
        public CleanOptions Clean { get; set; }

        [VerbOption("repair", HelpText = "Attempt various automatic repairs")]
        public SubCommandOptions Repair { get; set; }

        [VerbOption("repo", HelpText = "Manage CFAN repositories")]
        public SubCommandOptions KSP { get; set; }

        [VerbOption("factorio", HelpText = "Manage Factorio installs")]
        public SubCommandOptions Repo { get; set; }

        [VerbOption("compare", HelpText = "Compare version strings")]
        public CompareOptions Compare { get; set; }

        [VerbOption("version", HelpText = "Show the version of the CFAN client being used.")]
        public VersionOptions Version { get; set; }
    }

    // Options common to all classes.

    public class CommonOptions
    {
        [Option('v', "verbose", DefaultValue = false, HelpText = "Show more of what's going on when running.")]
        public bool Verbose { get; set; }

        [Option('d', "debug", DefaultValue = false, HelpText = "Show debugging level messages. Implies verbose")]
        public bool Debug { get; set; }

        [Option("debugger", DefaultValue = false, HelpText = "Launch debugger at start")]
        public bool Debugger { get; set; }

        [Option('f', "factorio", DefaultValue = null, HelpText = "Factorio install to use (by previously set name, see managing Factorio installs)")]
        public string FactorioInstallName { get; set; }

        [Option('i', "factorio-dir", DefaultValue = null, HelpText = "Path to Factorio directory to use")]
        public string FactorioDirectory { get; set; }

        [Option("net-useragent", DefaultValue = null, HelpText = "Set the default user-agent string for HTTP requests")]
        public string NetUserAgent { get; set; }

        [Option("headless", DefaultValue = null, HelpText = "Set to disable all prompts")]
        public bool Headless { get; set; }

        [Option("asroot", DefaultValue = null, HelpText = "Allows CFAN to run as root on Linux-based systems (bad idea)")]
        public bool AsRoot { get; set; }
    }

    /// <summary>
    /// For things which are subcommands ('factorio', 'repair' etc), we just grab a list
    /// we can pass on.
    /// </summary>
    public class SubCommandOptions : CommonOptions
    {
        [ValueList(typeof(List<string>))]
        public List<string> options { get; set; }
    }

    // Each action defines its own options that it supports.
    // Don't forget to cast to this type when you're processing them later on.

    internal class InstallOptions : CommonOptions
    {
        [OptionArray('c', "cfanfiles", HelpText = "Local CFAN files to process")]
        public string[] ckan_files { get; set; }

        [Option("no-recommends", HelpText = "Do not install recommended modules")]
        public bool no_recommends { get; set; }

        [Option("with-suggests", HelpText = "Install suggested modules")]
        public bool with_suggests { get; set; }

        [Option("with-all-suggests", HelpText = "Install suggested modules all the way down")]
        public bool with_all_suggests { get; set; }

        // TODO: How do we provide helptext on this?
        [ValueList(typeof (List<string>))]
        public List<string> modules { get; set; }
    }

    internal class UpgradeOptions : CommonOptions
    {
        [Option('c', "cfanfile", HelpText = "Local CFAN file to process")]
        public string ckan_file { get; set; }

        [Option("no-recommends", HelpText = "Do not install recommended modules")]
        public bool no_recommends { get; set; }

        [Option("with-suggests", HelpText = "Install suggested modules")]
        public bool with_suggests { get; set; }

        [Option("with-all-suggests", HelpText = "Install suggested modules all the way down")]
        public bool with_all_suggests { get; set; }

        [Option("all", HelpText = "Upgrade all available updated modules")]
        public bool upgrade_all { get; set; }

        // TODO: How do we provide helptext on this?
        [ValueList(typeof (List<string>))]
        public List<string> modules { get; set; }
    }

    internal class ScanOptions : CommonOptions
    {
    }

    internal class ListOptions : CommonOptions
    {
        [Option("porcelain", HelpText = "Dump raw list of modules, good for shell scripting")]
        public bool porcelain { get; set; }

        [Option("export", HelpText = "Export list of modules in specified format to stdout")]
        public string export { get; set; }
    }

    internal class VersionOptions : CommonOptions
    {
    }

    internal class CleanOptions : CommonOptions
    {
    }

    internal class AvailableOptions : CommonOptions
    {
    }

    internal class GuiOptions : CommonOptions
    {
        [Option("show-console", HelpText = "Shows the console while running the GUI")]
        public bool ShowConsole { get; set; }
    }

    internal class UpdateOptions : CommonOptions
    {
        // This option is really meant for devs testing their CFAN-meta forks.
        [Option('r', "repo", HelpText = "CFAN repository to use (experimental!)")]
        public string repo { get; set; }

        [Option("all", HelpText = "Upgrade all available updated modules")]
        public bool update_all { get; set; }

        [Option("list-changes", DefaultValue = false, HelpText = "List new and removed modules")]
        public bool list_changes { get; set; }
    }

    internal class RemoveOptions : CommonOptions
    {
        [Option("re", HelpText = "Parse arguments as regular expressions")]
        public bool regex { get; set; }

        [ValueList(typeof(List<string>))]
        public List<string> modules { get; set; }

        [Option("all", HelpText = "Remove all installed mods.")]
        public bool rmall { get; set; }
    }

    internal class ShowOptions : CommonOptions
    {
        [ValueOption(0)]
        public string Modname { get; set; }
    }

    internal class ClearCacheOptions : CommonOptions
    {
    }

    internal class SearchOptions : CommonOptions
    {
        [ValueOption(0)]
        public string search_term { get; set; }
    }

    internal class CompareOptions : CommonOptions
    {
        [ValueOption(0)]
        public string Left { get; set; }

        [ValueOption(1)]
        public string Right { get; set; }
    }
}
