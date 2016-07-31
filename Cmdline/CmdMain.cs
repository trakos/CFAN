// Reference CKAN client
// Paul '@pjf' Fenwick
//
// License: CC-BY 4.0, LGPL, or MIT (your choice)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CKAN.Factorio;
using log4net;
using log4net.Config;
using log4net.Core;

namespace CKAN.CmdLine
{
    public static class CmdMain
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CmdMain));

        [STAThread]
        public static int Main(string[] args)
        {
            int ret = RunCommandLine(args);
            return ret;
        }

        public static int RunCommandLine(string[] args, Func<string[], GuiOptions, int> showGuiFunc = null)
        {
            // Launch debugger if the "--debugger" flag is present in the command line arguments.
            // We want to do this as early as possible so just check the flag manually, rather than doing the
            // more robust argument parsing.
            if (args.Any(i => i == "--debugger"))
            {
                Debugger.Launch();
            }

            if (args.Length == 1 && args.Any(i => i == "--verbose" || i == "--debug"))
            {
                // Start the gui with logging enabled #437 
                List<string> guiCommand = args.ToList();
                guiCommand.Insert(0, "gui");
                guiCommand.Add("--show-console");
                args = guiCommand.ToArray();
            }

            BasicConfigurator.Configure();
            LogManager.GetRepository().Threshold = Level.Warn;
            log.Debug("CFAN started");

            Options cmdline;

            // If we're starting with no options then invoke the GUI instead (if compiled with GUI)
            if (args.Length == 0)
            {
                if (showGuiFunc != null)
                {
                    return showGuiFunc(args, null);
                }
                args = new[] {"--help"};
            }

            IUser user;
            try
            {
                cmdline = new Options(args);
            }
            catch (BadCommandKraken)
            {
                // Our help screen will already be shown. Let's add some extra data.
                user = new ConsoleUser(false);
                user.RaiseMessage("You are using CFAN version {0}", Meta.Version());

                return Exit.BADOPT;
            }

            // Process commandline options.

            var options = (CommonOptions)cmdline.options;
            user = new ConsoleUser(options.Headless);
            CheckMonoVersion(user, 3, 1, 0);

            if ((Platform.IsUnix || Platform.IsMac) && CmdLineUtil.GetUID() == 0)
            {
                if (!options.AsRoot)
                {
                    user.RaiseError(@"You are trying to run CFAN as root.
This is a bad idea and there is absolutely no good reason to do it. Please run CFAN from a user account (or use --asroot if you are feeling brave).");
                    return Exit.ERROR;
                }
                user.RaiseMessage("Warning: Running CFAN as root!");
            }

            if (options.Debug)
            {
                LogManager.GetRepository().Threshold = Level.Debug;
                log.Info("Debug logging enabled");
            }
            else if (options.Verbose)
            {
                LogManager.GetRepository().Threshold = Level.Info;
                log.Info("Verbose logging enabled");
            }

            // Assign user-agent string if user has given us one
            if (options.NetUserAgent != null)
            {
                Net.UserAgentString = options.NetUserAgent;
            }

            // User provided KSP instance

            if (options.FactorioDirectory != null && options.FactorioInstallName != null)
            {
                user.RaiseMessage("--factorio and --factorio-dir can't be specified at the same time");
                return Exit.BADOPT;
            }
            KSPManager manager = new KSPManager(user);
            if (options.FactorioInstallName != null)
            {
                // Set a KSP directory by its alias.

                try
                {
                    manager.SetCurrentInstance(options.FactorioInstallName);
                }
                catch (InvalidKSPInstanceKraken)
                {
                    user.RaiseMessage("Invalid Factorio installation specified \"{0}\", use '--factorio-dir' to specify by path, or 'list-installs' to see known Factorio installations", options.FactorioInstallName);
                    return Exit.BADOPT;
                }
            }
            else if (options.FactorioDirectory != null)
            {
                // Set a KSP directory by its path
                manager.SetCurrentInstanceByPath(options.FactorioDirectory);
            }
            else if (cmdline.action != "factorio" && cmdline.action != "version" && cmdline.action != "gui")
            {
                // Find whatever our preferred instance is.
                // We don't do this on `ksp/version` commands, they don't need it.
                CKAN.KSP ksp = manager.GetPreferredInstance();

                if (ksp == null)
                {
                    user.RaiseMessage("I don't know where Factorio is installed.");
                    user.RaiseMessage("Use 'cfan factorio help' for assistance on setting this.");
                    return Exit.ERROR;
                }
                log.InfoFormat("Using Factorio install at {0} with data dir set to {1}", ksp.GameDir(), ksp.GameData());

                if (ksp.lacksFactorioAuthData())
                {
                    user.RaiseError(
                        "Your config file located in {0} does not contain Factorio authorization data. Mods from official factorio.com mod portal will not be shown.\n\rYou can fix it by using in-game mod portal once. For headless you can copy values of service-username and service-token from your regular Factorio install.",
                        new object[] {ksp.getFactorioAuthDataPath()}
                        );
                }
            }

            switch (cmdline.action)
            {
                case "gui":
                    return ShowGui(args, user, (GuiOptions)options, showGuiFunc);

                case "version":
                    return Version(user);

                case "update":
                    return (new Update(user)).RunCommand(manager.CurrentInstance, (UpdateOptions)cmdline.options);

                case "available":
                    return Available(manager.CurrentInstance, user);

                case "install":
                    Scan(manager.CurrentInstance, user, cmdline.action);
                    return (new Install(user)).RunCommand(manager.CurrentInstance, (InstallOptions)cmdline.options);

                case "scan":
                    return Scan(manager.CurrentInstance, user);

                case "list":
                    return (new List(user)).RunCommand(manager.CurrentInstance, (ListOptions)cmdline.options);

                case "show":
                    return (new Show(user)).RunCommand(manager.CurrentInstance, (ShowOptions)cmdline.options);

                case "search":
                    return (new Search(user)).RunCommand(manager.CurrentInstance, options);

                case "remove":
                    return (new Remove(user)).RunCommand(manager.CurrentInstance, cmdline.options);

                case "upgrade":
                    Scan(manager.CurrentInstance, user, cmdline.action);
                    return (new Upgrade(user)).RunCommand(manager.CurrentInstance, cmdline.options);

                case "clean":
                    return Clean(manager.CurrentInstance);

                case "repair":
                    var repair = new Repair(manager.CurrentInstance, user);
                    return repair.RunSubCommand((SubCommandOptions)cmdline.options);

                case "factorio":
                    var ksp = new KSP(manager, user);
                    return ksp.RunSubCommand((SubCommandOptions)cmdline.options);

                case "repo":
                    var repo = new Repo(manager, user);
                    return repo.RunSubCommand((SubCommandOptions)cmdline.options);

                case "compare":
                    return (new Compare(user)).RunCommand(manager.CurrentInstance, cmdline.options);

                default:
                    user.RaiseMessage("Unknown command, try --help");
                    return Exit.BADOPT;
            }
        }

        private static void CheckMonoVersion(IUser user, int rec_major, int rec_minor, int rec_patch)
        {
            try
            {
                Type type = Type.GetType("Mono.Runtime");
                if (type == null) return;

                MethodInfo display_name = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (display_name != null)
                {
                    var version_string = (string)display_name.Invoke(null, null);
                    var match = Regex.Match(version_string, @"^\D*(?<major>[\d]+)\.(?<minor>\d+)\.(?<revision>\d+).*$");

                    if (match.Success)
                    {
                        int major = Int32.Parse(match.Groups["major"].Value);
                        int minor = Int32.Parse(match.Groups["minor"].Value);
                        int patch = Int32.Parse(match.Groups["revision"].Value);

                        if (major < rec_major || (major == rec_major && minor < rec_minor))
                        {
                            user.RaiseMessage(
                                "Warning. Detected mono runtime of {0} is less than the recommended version of {1}\n",
                                String.Join(".", major, minor, patch),
                                String.Join(".", rec_major, rec_minor, rec_patch)
                                );
                            user.RaiseMessage("Update recommend\n");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignored. This may be fragile and is just a warning method
            }
        }

        private static int ShowGui(string[] args, IUser user, GuiOptions options, Func<string[], GuiOptions, int> showGuiFunc)
        {
            if (showGuiFunc == null)
            {
                user.RaiseError("Error: option --gui not available, you have a headless exe.");
                return Exit.BADOPT;
            }

            // TODO: Sometimes when the GUI exits, we get a System.ArgumentException,
            // but trying to catch it here doesn't seem to help. Dunno why.
            return showGuiFunc(args, options);
        }

        private static int Version(IUser user)
        {
            user.RaiseMessage(Meta.Version());

            return Exit.OK;
        }

        private static int Available(CKAN.KSP current_instance, IUser user)
        {
            List<CfanModule> available = RegistryManager.Instance(current_instance).registry.Available(current_instance.Version());

            user.RaiseMessage("Mods available for Factorio {0}", current_instance.Version());
            user.RaiseMessage("");

            var width = user.WindowWidth;

            foreach (CfanModule module in available)
            {
                string entry = String.Format("* {0} ({1}) - {2}", module.identifier, module.modVersion, module.title);
                user.RaiseMessage(width > 0 ? entry.PadRight(width).Substring(0, width - 1) : entry);
            }

            return Exit.OK;
        }

        /// <summary>
        /// Scans the ksp instance. Detects installed mods to mark as auto-detected and checks the consistency
        /// </summary>
        /// <param name="ksp_instance">The instance to scan</param>
        /// <param name="user"></param>
        /// <param name="next_command">Changes the output message if set.</param>
        /// <returns>Exit.OK if instance is consistent, Exit.ERROR otherwise </returns>
        private static int Scan(CKAN.KSP ksp_instance, IUser user, string next_command = null)
        {
            try
            {
                ksp_instance.ScanGameData();
                return Exit.OK;
            }
            catch (InconsistentKraken kraken)
            {

                if (next_command == null)
                {
                    user.RaiseError(kraken.InconsistenciesPretty);
                    user.RaiseError("The repo has not been saved.");
                }
                else
                {
                    user.RaiseMessage("Preliminary scanning shows that the install is in a inconsistent state.");
                    user.RaiseMessage("Use cfan.exe scan for more details");
                    user.RaiseMessage("Proceeding with {0} in case it fixes it.\n", next_command);
                }

                return Exit.ERROR;
            }
        }

        private static int Clean(CKAN.KSP current_instance)
        {
            current_instance.CleanCache();
            return Exit.OK;
        }
    }

    public class CmdLineUtil
    {
        public static uint GetUID()
        {
            if (Platform.IsUnix || Platform.IsMac)
            {
                return getuid();
            }

            return 1;
        }

        [DllImport("libc")]
        private static extern uint getuid();
    }
}
