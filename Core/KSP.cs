using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Transactions;
using CKAN.Factorio;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;
using log4net;
using Newtonsoft.Json;
using static System.String;

[assembly: InternalsVisibleTo("CKAN.Tests")]

namespace CKAN
{
    
    /// <summary>
    ///     Everything for dealing with KSP itself.
    /// </summary>
    public class KSP : IDisposable
    {
        public IUser User { get; set; }

        #region Fields and Properties

        private static readonly ILog log = LogManager.GetLogger(typeof(KSP));

        private readonly string gamedir;
        private readonly string gamedatadir;
        private FactorioVersion version;

        public NetFileCache Cache { get; private set; }

        public RegistryManager RegistryManager => RegistryManager.Instance(this);

        public Registry Registry
        {
            get { return RegistryManager.registry; }
        }

        #endregion
        #region Construction and Initialisation

        /// <summary>
        /// Returns a KSP object, insisting that directory contains a valid KSP install.
        /// Will initialise a CKAN instance in the KSP dir if it does not already exist.
        /// Throws a NotFactorioDirectoryKraken if directory is not a KSP install.
        /// </summary>
        public KSP(string directory, IUser user)
        {
            User = user;

            // Make sure our path is absolute and has normalised slashes.
            directory = KSPPathUtils.NormalizePath(Path.GetFullPath(directory));

            VerifyFactorioDirectory(directory);
            
            gamedir = directory;
            gamedatadir = DetectGameDataDirectory(gamedir);
            VerifyFactorioDataDirectory(gamedatadir);
            Init();
            Cache = new NetFileCache(DownloadCacheDir());
        }

        /// <summary>
        ///     Create the CKAN directory and any supporting files.
        /// </summary>
        private void Init()
        {
            log.DebugFormat("Initialising {0}", CkanDir());

            if (!Directory.Exists(Mods()))
            {
                User.RaiseMessage("Creating {0}", Mods());
                Directory.CreateDirectory(Mods());
            }

            if (! Directory.Exists(CkanDir()))
            {
                User.RaiseMessage("Setting up CFAN for the first time...");
                User.RaiseMessage("Creating {0}", CkanDir());
                Directory.CreateDirectory(CkanDir());

                User.RaiseMessage("Scanning for installed mods...");
                ScanGameData();
            }

            if (! Directory.Exists(DownloadCacheDir()))
            {
                User.RaiseMessage("Creating {0}", DownloadCacheDir());
                Directory.CreateDirectory(DownloadCacheDir());
            }

            // Clear any temporary files we find. If the directory
            // doesn't exist, then no sweat; FilesystemTransaction
            // will auto-create it as needed.
            // Create our temporary directories, or clear them if they
            // already exist.
            if (Directory.Exists(TempDir()))
            {
                var directory = new DirectoryInfo(TempDir());
                foreach (FileInfo file in directory.GetFiles()) file.Delete();
                foreach (DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
            }

            log.DebugFormat("Initialised {0}", CkanDir());
        }

        /// <summary>
        /// Releases all resource used by the <see cref="CKAN.KSP"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CKAN.KSP"/>. The <see cref="Dispose"/>
        /// method leaves the <see cref="CKAN.KSP"/> in an unusable state. After calling <see cref="Dispose"/>, you must
        /// release all references to the <see cref="CKAN.KSP"/> so the garbage collector can reclaim the memory that
        /// the <see cref="CKAN.KSP"/> was occupying.</remarks>
        public void Dispose()
        {
            if (Cache != null)
                Cache.Dispose();
        }

        #endregion

        #region KSP Directory Detection and Versioning

        /// <summary>
        /// Returns the path to our portable version of KSP if ckan.exe is in the same
        /// directory as the game. Otherwise, returns null.
        /// </summary>
        public static string PortableDir()
        {
            // Find the directory our executable is stored in.
            // In Perl, this is just `use FindBin qw($Bin);` Verbose enough, C#?
            string exe_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            log.DebugFormat("Checking if Factorio is in my exe dir: {0}", exe_dir);

            // This verification implementation is cool, but better are welcome.
            if (IsFactorioDirectory(exe_dir))
            {
                log.InfoFormat("Factorio found at {0}", exe_dir);
                return exe_dir;
            }

            return null;
        }

        /// <summary>
        /// Attempts to automatically find a KSP install on this system.
        /// Returns the path to the install on success.
        /// Throws a DirectoryNotFoundException on failure.
        /// </summary>
        public static string FindGameDir()
        {
            // See if we can find KSP as part of a Steam install.
            string ksp_steam_path = KSPPathUtils.KSPSteamPath();

            if (ksp_steam_path != null)
            {
                if (IsFactorioDirectory(ksp_steam_path))
                {
                    return ksp_steam_path;
                }

                log.DebugFormat("Have Steam, but Factorio is not at \"{0}\".", ksp_steam_path);
            }

            string autodetected = KSPPathUtils.DefaultPath();

            if (autodetected != null)
            {
                if (IsFactorioDirectory(autodetected))
                {
                    return autodetected;
                }

                log.DebugFormat("Directory exists, but Factorio is not at \"{0}\".", ksp_steam_path);
            }

            // Oh noes! We can't find KSP!
            throw new DirectoryNotFoundException();
        }

        internal static bool IsFactorioDirectory(string directory)
        {
            try
            {
                VerifyFactorioDirectory(directory);
                log.DebugFormat("Found factorio directory: {0}", directory);
                return true;
            }
            catch (NotFactorioDirectoryKraken e)
            {
                if (!IsNullOrEmpty(e.Message))
                {
                    log.DebugFormat("Directory is not a Factorio root: {0}", e.Message);
                }
            }
            catch (NotFactorioDataDirectoryKraken e)
            {
                if (!IsNullOrEmpty(e.Message))
                {
                    log.WarnFormat("Directory is a factorio root, but failed to find data directory: {0}", e.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the specified directory looks like a Factorio directory.
        /// Returns true if found, false if not.
        /// </summary>
        internal static void VerifyFactorioDirectory(string directory)
        {
            //first we need to check is directory exists
            if (!Directory.Exists(Path.Combine(directory, "data")))
            {
                throw new NotFactorioDirectoryKraken(directory, $"Cannot find data in {directory}");
            }
            
            if (!Directory.Exists(Path.Combine(directory, "data", "base")))
            {
                throw new NotFactorioDirectoryKraken(directory, $"Cannot find data/base in {directory}");
            }

            if (!File.Exists(Path.Combine(directory, "data", "base", "info.json")))
            {
                throw new NotFactorioDirectoryKraken(directory, $"Cannot find data/base/info.json in {directory}");
            }

            //If both exist we should be able to get game version
            DetectVersion(directory);

            log.DebugFormat("{0} looks like a GameDir", directory);
        }

        /// <summary>
        /// Checks if the specified directory looks like a Factorio data directory.
        /// Returns true if found, false if not.
        /// </summary>
        internal static void VerifyFactorioDataDirectory(string directory)
        {
            // Checking for a config directory probably isn't the best way to
            // detect Factorio data directory, but it works. More robust implementations welcome.

            //first we need to check is directory exists
            if (!Directory.Exists(Path.Combine(directory, "config")))
            {
                throw new NotFactorioDataDirectoryKraken(directory,
                    $"Cannot find config in {directory}, did you start the game at least once?");
            }

            if (!File.Exists(Path.Combine(directory, "config", "config.ini")))
            {
                throw new NotFactorioDataDirectoryKraken(directory,
                    $"Cannot find config/config.ini in {directory}, did you start the game at least once?");
            }

            log.DebugFormat("{0} looks like a Factorio data directory", directory);
        }

        /// <summary>
        /// Detects the version of KSP in a given directory.
        /// Throws a NotKSPDirKraken if anything goes wrong.
        /// </summary>
        private static FactorioVersion DetectVersion(string directory)
        {
            //Contract.Requires<ArgumentNullException>(directory==null);

            ModInfoJson factorioBaseInfo;
            try
            {
                // Slurp our info.json into memory
                string json = File.ReadAllText(Path.Combine(directory, "data", "base", "info.json"));
                factorioBaseInfo = JsonConvert.DeserializeObject<ModInfoJson>(json);
            }
            catch
            {
                log.Error("Could not open and/or parse Factorio info.json in " + Path.Combine(directory, "data", "base"));
                throw new NotFactorioDirectoryKraken(directory, "info.json not found or not readable or not parsable");
            }

            return new FactorioVersion(factorioBaseInfo.version.ToString());
        }

        public void RebuildKSPSubDir()
        {
            string[] FoldersToCheck = { "scenario", "mods" };
            foreach (string sRelativePath in FoldersToCheck)
            {
                string sAbsolutePath = ToAbsoluteGameDataDir(sRelativePath);
                if (!Directory.Exists(sAbsolutePath))
                    Directory.CreateDirectory(sAbsolutePath);
            }
        }

        private static string DetectGameDataDirectory(string gameDirectory)
        {
            bool usesSystemDirectory = DetectIfGameUsesSystemDirectory(gameDirectory);

            if (!usesSystemDirectory)
            {
                return gameDirectory;
            }
            if (Platform.IsWindows)
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Factorio"
                );
            }
            if (Platform.IsMac)
            {
                string homeDirectory = Environment.GetEnvironmentVariable("HOME");
                if (IsNullOrEmpty(homeDirectory))
                {
                    throw new NotFactorioDirectoryKraken(gameDirectory, "Can't find HOME environment variable value");
                }
                return Path.Combine(homeDirectory, "Library", "Application Support", "factorio");
            }
            if (Platform.IsUnix)
            {
                string homeDirectory = Environment.GetEnvironmentVariable("HOME");
                if (IsNullOrEmpty(homeDirectory))
                {
                    throw new NotFactorioDirectoryKraken(gameDirectory, "Can't find HOME environment variable value");
                }
                return Path.Combine(homeDirectory, ".factorio");
            }
            throw new NotFactorioDirectoryKraken(gameDirectory, "Failed to recognize your OS, that's really unexpected and unfortunate, sorry!");
        }

        private static bool DetectIfGameUsesSystemDirectory(string gameDirectory)
        {
            string config;
            try
            {
                config = File.ReadAllText(Path.Combine(gameDirectory, "config-path.cfg"));
            }
            catch (Exception e)
            {
                log.Info("Could not open config-path.cfg in " + gameDirectory, e);
                return true;
            }

            Match match = Regex.Match(config, @"^use-system-read-write-data-directories=([\w]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success)
            {
                string useSystemDirValue = match.Groups[1].Value;
                log.DebugFormat("Found use-system-read-write-data-directories value: {0}", useSystemDirValue);
                bool isTrue = string.Equals(useSystemDirValue, "true", StringComparison.OrdinalIgnoreCase);
                bool isFalse = string.Equals(useSystemDirValue, "false", StringComparison.OrdinalIgnoreCase);
                if (!isTrue && !isFalse)
                {
                    throw new NotFactorioDirectoryKraken(
                        gameDirectory,
                        "can't parse use-system-read-write-data-directories value in config-path.cfg: " +
                        useSystemDirValue
                    );
                }

                return isTrue;
            }

            // Oh noes! We couldn't find the use-system-read-write-data-directories value!
            log.Error("Could not find use-system-read-write-data-directories in config-path.cfg");

            throw new NotFactorioDirectoryKraken(gameDirectory, "Could not find whether Factorio uses system dir in config-path.cfg");
        }

        #endregion

        #region Things which would be better as Properties

        public string GameDir()
        {
            return gamedir;
        }

        public string GameData()
        {
            return gamedatadir;
        }

        public string CkanDir()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(GameData(), "CFAN")
            );
        }

        public string DownloadCacheDir()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(CkanDir(), "downloads")
            );
        }

        public string Mods()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(GameData(), "mods")
            );
        }

        public string Scenarios()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(GameData(), "scenario")
            );
        }

        public string TempDir()
        {
            return KSPPathUtils.NormalizePath(
                Path.Combine(CkanDir(), "temp")
            );
        }

        public FactorioVersion Version()
        {
            if (version != null)
            {
                return version;
            }

            return version = DetectVersion(GameDir());
        }

        #endregion

        #region CKAN/GameData Directory Maintenance

        /// <summary>
        /// Removes all files from the download (cache) directory.
        /// </summary>
        public void CleanCache()
        {
            // TODO: We really should be asking our Cache object to do the
            // cleaning, rather than doing it ourselves.
            
            log.Debug("Cleaning cache directory");

            string[] files = Directory.GetFiles(DownloadCacheDir(), "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    log.DebugFormat("Skipping directory: {0}", file);
                    continue;
                }

                log.DebugFormat("Deleting {0}", file);
                File.Delete(file);
            }
        }

        /// <summary>
        /// Clears the registry of DLL data, and refreshes it by scanning GameData.
        /// This operates as a transaction.
        /// This *saves* the registry upon completion.
        /// </summary>
        // TODO: This would likely be better in the Registry class itself.
        public void ScanGameData()
        {
            using (TransactionScope tx = CkanTransaction.CreateTransactionScope())
            {
                Registry.ClearPreexistingModules();

                foreach (var detectedModule in FactorioModDetector.findAllModsInDirectory(Path.Combine(gamedatadir, "mods")))
                {
                    string detectedModulePath = detectedModule.Key;
                    ModInfoJson detectedModInfo = detectedModule.Value;
                    if (Registry.InstalledModules.Any(p => p.identifier == detectedModInfo.name))
                    {
                        continue;
                    }
                    AvailableModule availableModule;
                    if (Registry.available_modules.TryGetValue(detectedModInfo.name, out availableModule))
                    {
                        CfanModule availableCfan = availableModule.ByVersion(detectedModInfo.version);
                        if (availableCfan != null)
                        {
                            string expectedFilename = availableCfan.standardFileName + ".zip";
                            if (Path.GetFileName(detectedModulePath) == expectedFilename)
                            {
                                // yay, we can use this mod as installed (we will be able to update/remove it through cfan)
                                Registry.RegisterModule(availableCfan, new []{detectedModulePath}, this);
                                continue;
                            }
                        }
                    }
                    // we only register that this module exists, but we won't be able to do anything with it
                    Registry.RegisterPreexistingModule(this, detectedModulePath, detectedModInfo);
                }
                    
                tx.Complete();
            }
            RegistryManager.Save();
        }

        #endregion

        /// <summary>
        /// Returns path relative to this KSP's GameDir.
        /// </summary>
        public string ToRelativeGameDir(string path)
        {
            return KSPPathUtils.ToRelative(path, GameDir());
        }

        public string ToRelativeGameDataDir(string path)
        {
            return KSPPathUtils.ToRelative(path, GameData());
        }

        /// <summary>
        /// Given a path relative to this KSP's GameDir, returns the
        /// absolute path on the system. 
        /// </summary>
        public string ToAbsoluteGameDataDir(string path)
        {
            return KSPPathUtils.ToAbsolute(path, GameData());
        }

        public override string ToString()
        {
            return "Factorio Install:" + gamedir;
        }

        public override bool Equals(object obj)
        {
            var other = obj as KSP;
            return other != null ? gamedir.Equals(other.GameDir()) : base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return gamedir.GetHashCode();
        }

        public string getModTypeRootDirectory(CfanJson.CfanModType kind)
        {
            switch (kind)
            {
                case CfanJson.CfanModType.MOD:
                    return Mods();
                case CfanJson.CfanModType.TEXTURES:
                    throw new NotImplementedException();
                case CfanJson.CfanModType.META:
                    throw new NotImplementedException();
                case CfanJson.CfanModType.SCENARIO:
                    return Scenarios();
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public void RebuildFactorioModlist()
        {
            IList<ModListJson.ModListJsonItem> prevModList = tryGetPreviousFactorioModlist();
            ModListJson.ModListJsonItem dummyDisabledItem = new ModListJson.ModListJsonItem()
            {
                enabled = ModListJson.ModListJsonTruthy.YES
            };
            // if mod is already in mod-list.json, take its state from there
            // if not, set to enabled
            ModListJson modList = new ModListJson
            {
                mods = RegistryManager.registry.Installed(false)
                    .Keys.Select(
                        modName =>
                            new ModListJson.ModListJsonItem()
                            {
                                name = modName,
                                enabled =
                                    (prevModList.FirstOrDefault(p => p.name == modName) ?? dummyDisabledItem).enabled
                            }).ToList()
            };
            File.WriteAllText(Path.Combine(Mods(), "mod-list.json"), JsonConvert.SerializeObject(modList));
        }

        private IList<ModListJson.ModListJsonItem> tryGetPreviousFactorioModlist()
        {
            try
            {
                ModListJson prevModListJson = JsonConvert.DeserializeObject<ModListJson>(File.ReadAllText(Path.Combine(Mods(), "mod-list.json")));
                if (prevModListJson?.mods != null)
                {
                    return prevModListJson.mods;
                }
            }
            catch (Exception e)
            {
                log.Error("Couldn't read mod-list.json", e);
            }
            return new List<ModListJson.ModListJsonItem>();
        }

        public string findFactorioBinaryPath()
        {
            // pairs: <StringToSaveWhenFound, pathToCheck>
            // string to save can be relative
            Tuple<string, string>[] possibleLocations = new[]
            {
                new Tuple<string, string>(@"bin\x64\Factorio.exe", Path.Combine(GameDir(), @"bin\x64\Factorio.exe")),
                new Tuple<string, string>(@"bin\Win32\Factorio.exe", Path.Combine(GameDir(), @"bin\Win32\Factorio.exe")),
                new Tuple<string, string>(@"./bin/x64/Factorio", Path.Combine(GameDir(), @"bin\x64\Factorio")),
                new Tuple<string, string>(@"./bin/i386/Factorio", Path.Combine(GameDir(), @"bin\i386\Factorio")),
                new Tuple<string, string>(@"./MacOS/factorio", Path.Combine(GameDir(), @"MacOS/factorio")),
                new Tuple<string, string>(@"/usr/bin/factorio", Path.Combine(GameDir(), @"/usr/bin/factorio")),
            };

            Tuple<string, string> foundLocation = possibleLocations.FirstOrDefault(possibleLocation => File.Exists(possibleLocation.Item2));
            return foundLocation != null ? foundLocation.Item1 : "";
        }
    }
}
