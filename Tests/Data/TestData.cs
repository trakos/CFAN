using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CKAN;
using CKAN.Factorio;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;
using Newtonsoft.Json;

namespace Tests.Data
{
    static public class TestData
    {
        public static string DataDir()
        {
            // TODO: Have this actually walk our directory structure and find
            // t/data. This means we can relocate our test executable and
            // things will still work.
            string current = Directory.GetCurrentDirectory();

            return Path.Combine(current, "../../Data");
        }

        public static string DataDir(string file)
        {
            return Path.Combine(DataDir(), file);
        }

        public static string GithubEmptyAssetsJsonFilePath()
        {
            return Path.Combine(DataDir(), "empty_assets.json");
        }

        public static string GithubAssetJsonFilePath()
        {
            return Path.Combine(DataDir(), "assets.json");
        }

        /// <summary>
        /// Returns the full path to DogeCoinFlag-1.01.zip
        /// </summary>
        public static string DogeCoinFlagZip()
        {
            return Path.Combine(DataDir(), "DoubleFurnace-0.1.2.zip");
        }

        /// <summary>
        /// Returns the full path to DogeCoinFlag-1.01-corrupt.zip
        /// </summary>
        public static string DogeCoinFlagZipCorrupt()
        {
            string such_zip_very_corrupt_wow = Path.Combine(DataDir(), "DogeCoinFlag-1.01-corrupt.zip");

            return such_zip_very_corrupt_wow;
        }

        public static string DogeCoinFlag_101()
        {
            return @"{
               ""modInfo"": {
                  ""name"": ""DoubleFurnace"",
                  ""version"": ""0.1.2"",
                  ""title"": ""Double Furnace"",
                  ""author"": ""raid"",
                  ""contact"": ""mod@hackmate.de"",
                  ""homepage"": ""http://www.factoriomods.com/mods/double-furnace"",
                  ""description"": ""Mod that adds a Double Furnace to Factorio. A double furnace smelts iron ore directly to steel plates."",
                  ""dependencies"": [
                     ""base >= 0.12.17""
                  ]
                },
               ""authors"": [
                  ""raid""
               ],
               ""categories"": [],
               ""tags"": [],
               ""suggests"": [],
               ""recommends"": [],
               ""conflicts"": [],
               ""downloadUrls"": [
                  ""http://cfan.trakos.pl/repo/mods-fmm/DoubleFurnace_0.1.2.zip""
               ],
               ""downloadSize"": 147237,
               ""type"": ""MOD"",
               ""releasedAt"": null,
               ""aggregatorData"": {
                  ""x-source"": ""FactorioModsComAggregator"",
                  ""fmm-id"": ""229""
               }
            }";
        }

        public static string DogeCoinFlag_101_bugged_module()
        {
            return @"{
               ""modInfo"": {
                  ""name"": ""DoubleFurnace"",
                  ""version"": ""0:14^0"",
                  ""title"": ""Double Furnace"",
                  ""author"": ""raid"",
                  ""contact"": ""mod@hackmate.de"",
                  ""homepage"": ""http://www.factoriomods.com/mods/double-furnace"",
                  ""description"": ""Mod that adds a Double Furnace to Factorio. A double furnace smelts iron ore directly to steel plates."",
                  ""dependencies"": [
                     ""base >= 0.12.17""
                  ]
                },
               ""authors"": [
                  ""raid""
               ],
               ""categories"": [],
               ""tags"": [],
               ""suggests"": [],
               ""recommends"": [],
               ""conflicts"": [],
               ""downloadUrls"": [
                  ""http://cfan.trakos.pl/repo/mods-fmm/DoubleFurnace_0.1.2.zip""
               ],
               ""downloadSize"": 147237,
               ""type"": ""MOD"",
               ""releasedAt"": null,
               ""aggregatorData"": {
                  ""x-source"": ""FactorioModsComAggregator"",
                  ""fmm-id"": ""229""
               }
            }";
        }

        public static CfanModule DogeCoinFlag_101_module()
        {
            return CfanFileManager.fromJson(DogeCoinFlag_101());
        }

        // Identical to DogeCoinFlag_101, but with a spec version over 9000!
        public static string FutureMetaData()
        {
            return @"
                {
                    ""spec_version"": ""v9000.0.1"",
                    ""identifier"": ""DogeCoinFlag"",
                    ""install"": [
                        {
                        ""file"": ""DogeCoinFlag-1.01/GameData/DogeCoinFlag"",
                        ""install_to"": ""GameData"",
                        ""filter"" : [ ""Thumbs.db"", ""README.md"" ],
                        ""filter_regexp"" : ""\\.bak$""
                        }
                    ],
                    ""resources"": {
                        ""kerbalstuff"": {
                        ""url"": ""https://kerbalstuff.com/mod/269/Dogecoin%20Flag""
                        },
                        ""homepage"": ""https://www.reddit.com/r/dogecoin/comments/1tdlgg/i_made_a_more_accurate_dogecoin_and_a_ksp_flag/""
                    },
                    ""name"": ""Dogecoin Flag"",
                    ""license"": ""CC-BY"",
                    ""abstract"": ""Such flag. Very currency. To the mun! Wow!"",
                    ""author"": ""pjf"",
                    ""version"": ""1.01"",
                    ""download"": ""https://kerbalstuff.com/mod/269/Dogecoin%20Flag/download/1.01"",
                    ""comment"": ""Generated by ks2ckan"",
                    ""download_size"": 53647,
                    ""ksp_version"": ""0.25""
                }
            ";
        }

        /// <summary>
        /// Taurus HCV pod, which seems to cause weird KS errors when the unescaped
        /// download string is used.
        /// </summary>
        public static string RandSCapsuleDyne()
        {
            return @"
                {
                    ""spec_version"": 1,
                    ""name"": ""Taurus HCV - 3.75 m Stock-ish Crew Pod"",
                    ""identifier"": ""RandSCapsuledyne"",
                    ""license"": ""CC-BY-SA-3.0"",
                    ""install"": [
                        {
                            ""file"": ""GameData/R&SCapsuledyne"",
                            ""install_to"": ""GameData""
                        }
                    ],
                    ""depends"": [
                        {
                            ""name"": ""BDAnimationModules""
                        }
                    ],
                    ""resources"": {
                        ""homepage"": ""http://forum.kerbalspaceprogram.com/threads/75074-Taurus-HCV-3-75-m-Stock-ish-Crew-Pod-v-b0-5-April-4-2014?p=1064792#post1064792"",
                        ""kerbalstuff"": ""https://kerbalstuff.com/mod/13/Taurus%20HCV%20-%203.75%20m%20Stock-ish%20Crew%20Pod""
                    },
                    ""ksp_version"": ""0.90"",
                    ""abstract"": ""0.90.0 COMPATIBLE! The Taurus High Capacity Vehicle is a 7 kerbal, 3.75-m cockpit designed to integrate well with the stock game. "",
                    ""author"": ""jnrobinson"",
                    ""version"": ""1.4.0"",
                    ""download"": ""https://kerbalstuff.com/mod/13/Taurus%20HCV%20-%203.75%20m%20Stock-ish%20Crew%20Pod/download/1.4.0"",
                    ""x_generated_by"": ""netkan"",
                    ""download_size"": 8351916
                }
            ";
        }

        public static CfanModule RandSCapsuleDyneModule()
        {
            var cfan = JsonConvert.DeserializeObject<CfanJson>(RandSCapsuleDyne());
            return new CfanModule(cfan);
        }

        // TestKAN in tar.gz format.
        public static Uri TestKANTarGz()
        {
            return new Uri(DataDir("CFAN-repository.tar.gz"), UriKind.Relative);
        }

        // TestKAN in zip format.
        public static Uri TestKANZip()
        {
            return new Uri(DataDir("CFAN-repository.zip"), UriKind.Relative);
        }

        // A repo full of deliciously bad metadata in tar.gz format.
        public static Uri BadKANTarGz()
        {
            return new Uri(DataDir("CKAN-meta-badkan.tar.gz"));
        }

        // A repo full of deliciously bad metadata in zip format.
        public static Uri BadKANZip()
        {
            return new Uri(DataDir("CKAN-meta-badkan.zip"));
        }

        public static string good_factorio_dir()
        {
            return Path.Combine(DataDir(), "factorio/Factorio_0.12.29");
        }

        public static IEnumerable<string> bad_ksp_dirs()
        {
            var dirs = new List<string>
            {
                Path.Combine(DataDir(), "KSP/bad-ksp"),
                Path.Combine(DataDir(), "KSP/missing-gamedata")
            };

            return dirs;
        }

        public static string kOS_014()
        {
            return @"{""modInfo"":{""name"":""FARL"",""version"":""2.5.0"",""title"":""Fully Automated Rail Layer"",""author"":""Choumiko"",""contact"":""www.factorioforums.com"",""homepage"":null,""description"":""Fully automated rail - layer"",""dependencies"":[""base >= 0.12.32"",""? 5dim_trains"",""? TheFatController""]},""authors"":[""Choumiko""],""categories"":[],""tags"":[],""suggests"":[],""recommends"":[],""conflicts"":[],""downloadUrls"":[""http://cfan.trakos.pl/repo/mods-fmm/FARL_0.5.25.zip""],""downloadSize"":10629751,""type"":""MOD"",""releasedAt"":null,""aggregatorData"":{""x-source"":""FactorioModsComAggregator"",""fmm-id"":""271"",""github-repo"":""Choumiko/FARL""}}";
        }

        public static CfanModule kOS_014_module()
        {
            return CfanFileManager.fromJson(kOS_014());
        }

        public static string KS_CustomAsteroids_string()
        {
            return File.ReadAllText(Path.Combine(DataDir(), "KS/CustomAsteroids.json"));
        }

        public static CfanModule RsoModule()
        {
            return CfanFileManager.fromCfanFile(Path.Combine(DataDir(), "rso-mod_1.5.1.cfan"));
        }

        public static string KspAvcJson()
        {
            return File.ReadAllText(Path.Combine(DataDir(), "ksp-avc.version"));
        }


        public static string KspAvcJsonOneLineVersion()
        {
            return File.ReadAllText(Path.Combine(DataDir(), "ksp-avc-one-line.version"));
        }

        public static CfanModule ModuleManagerModule()
        {
            return CfanFileManager.fromCfanFile(DataDir("FARL_0.2.5.cfan"));
        }

        public static string ModuleManagerZip()
        {
            return DataDir("FARL_0.2.5.zip");
        }

        /// <summary>
        /// A path to our test registry.json file. Please copy before using.
        /// </summary>
        public static string TestRegistry()
        {
            return DataDir("registry.json");
        }

        // Where's my mkdtemp? Instead we'll make a random file, delete it, and
        // fill its place with a directory.
        // Taken from https://stackoverflow.com/a/20445952
        public static string NewTempDir()
        {
            string temp_folder = Path.GetTempFileName();
            File.Delete(temp_folder);
            Directory.CreateDirectory(temp_folder);

            return temp_folder;
        }

        // Ugh, this is awful.
        public static void CopyDirectory(string src, string dst)
        {
            // Create directory structure
            foreach (string path in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(path.Replace(src, dst));
            }

            // Copy files.
            foreach (string file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(src, dst));
            }
        }

        public static string ConfigurationFile()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
            <Configuration xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
              <CommandLineArguments>KSP.exe -force-opengl</CommandLineArguments>
              <AutoCloseWaitDialog>false</AutoCloseWaitDialog>
              <URLHandlerNoNag>false</URLHandlerNoNag>
              <CheckForUpdatesOnLaunch>true</CheckForUpdatesOnLaunch>
              <CheckForUpdatesOnLaunchNoNag>true</CheckForUpdatesOnLaunchNoNag>
              <SortByColumnIndex>2</SortByColumnIndex>
              <SortDescending>false</SortDescending>
              <WindowSize>
                <Width>1024</Width>
                <Height>664</Height>
              </WindowSize>
              <WindowLoc>
                <X>512</X>
                <Y>136</Y>
              </WindowLoc>
            </Configuration>";
        }
    }

    public class RandomModuleGenerator
    {
        public Random Generator { get; set; }

        public RandomModuleGenerator(Random generator)
        {
            Generator = generator;
        }

        public CfanModule GeneratorRandomModule(
            List<ModDependency> conflicts = null,
            List<ModDependency> depends = null,
            List<ModDependency> sugests = null,
            List<String> provides = null,
            List<ModDependency> recommends = null,
            string identifier = null,
            Version version = null)
        {
            if (depends == null)
            {
                depends = new List<ModDependency>();
            }
            if (depends.All(p => p.modName != "base"))
            {
                depends.Add(new ModDependency("base"));
            }

            var cfanJson = new CfanJson()
            {
                modInfo = new ModInfoJson()
                {
                    dependencies = depends.ToArray(),
                    version = new ModVersion(version?.ToString() ?? "0.0.1"),
                    name = identifier ?? Generator.Next().ToString(CultureInfo.InvariantCulture),
                    description = Generator.Next().ToString(CultureInfo.InvariantCulture),
                    title = Generator.Next().ToString(CultureInfo.InvariantCulture)
                },
                conflicts = conflicts?.ToArray(),
                suggests = sugests?.ToArray()
            };
            return new CfanModule(cfanJson);
        }
    }
}

