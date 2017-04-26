using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CFAN_netfan.CfanAggregator;
using CFAN_netfan.CfanAggregator.Aggregators;
using CFAN_netfan.CfanAggregator.Github;
using CFAN_netfan.CfanAggregator.ModFileNormalizer;
using CFAN_netfan.Compression;
using CKAN;
using CKAN.CmdLine;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using Newtonsoft.Json;

namespace CFAN_netfan
{
    class Program
    {
        private static string repoPath;
        private static string repoUrlPrefix;
        private static string githubAccessToken;
        private static string outputPath => Path.Combine(repoPath, "cfans");
        private static string outputVersion2Path => Path.Combine(repoPath, "cfans_v2");
        private static string repositoryTarGz => Path.Combine(repoPath, "repository.tar.gz");
        private static string repositoryVersion2TarGz => Path.Combine(repoPath, "repository_v2.tar.gz");
        private static string repositoryTemporaryTarGz => Path.Combine(repoPath, "repository_tmp.tar.gz");


        [STAThread]
        static void Main(string[] args)
        {
            IUser user = new ConsoleUser(false);
            repoPath = args[0];
            repoUrlPrefix = args[1];
            githubAccessToken = args[2];

            Directory.CreateDirectory(Path.Combine(repoPath, "cache"));
            NetFileCache netFileCache = new NetFileCache(Path.Combine(repoPath, "cache"));
            CombinedModFileNormalizer modFileNormalizer = new CombinedModFileNormalizer(new IModFileNormalizer[]
            {
                new RarToZipNormalizer(), 
                new SevenZipToZipNormalizer(),
                new ModZipRootNormalizer()
            });
            ModDirectoryManager manualModDirectoryManager = new ModDirectoryManager(repoUrlPrefix, repoPath, "mods", modFileNormalizer, netFileCache);
            ModDirectoryManager githubModsDirectoryManager = new ModDirectoryManager(repoUrlPrefix, repoPath, "mods-github", modFileNormalizer, netFileCache);
            CombinedCfanAggregator combinedAggregator = new CombinedCfanAggregator(new ICfanAggregator[]
            {
                new LocalRepositoryAggregator(manualModDirectoryManager),
                new GithubAggregator(githubModsDirectoryManager, new GithubRepositoriesDataProvider(), githubAccessToken),
                new FactorioComAggregator(),
            });
            var cfanJsons = combinedAggregator.getAllCfanJsons(user).ToList();
            cfanJsons.Where(p => !CfanJson.requiresFactorioComAuthorization(p)).ToList().ForEach(p => saveCfanJson(user, p));
            cfanJsons.ForEach(p => saveCfanJson(user, p, v2: true));
            createFinalRepositoryTarGz(user);
            createFinalRepositoryTarGz(user, v2: true);
            user.RaiseMessage("Done.");
        }

        protected static void saveCfanJson(IUser user, CfanJson cfanJson, bool v2 = false)
        {
            string subdirectory = cfanJson.modInfo.name;
            string outputPath = v2 ? Program.outputVersion2Path : Program.outputPath;
            string filename = cfanJson.modInfo.name + "_" + cfanJson.modInfo.version + ".cfan";
            var previousDependencies = cfanJson.modInfo.dependencies;
            if (!v2)
            {
                cfanJson.modInfo.dependencies = previousDependencies.ToList().Select(createLegacyDependency).ToArray();
            }
            Directory.CreateDirectory(Path.Combine(outputPath, subdirectory));
            File.WriteAllText(
                Path.Combine(outputPath, subdirectory, filename),
                JsonConvert.SerializeObject(cfanJson)
            );
            if (!v2)
            {
                cfanJson.modInfo.dependencies = previousDependencies;
            }
            user.RaiseMessage($"Generated {filename}");
        }

        protected static ModDependency createLegacyDependency(ModDependency dependency)
        {
            var minVersion = dependency.minVersion;
            var maxVersion = dependency.maxVersion;
            if (minVersion != null && maxVersion != null)
            {
                // old CFAN versions didn't support setting both minVersion and maxVersion
                minVersion = null;
            }
            return new ModDependency(minVersion, maxVersion, dependency.modName, dependency.isOptional);
        }

        protected static void createFinalRepositoryTarGz(IUser user, bool v2 = false)
        {
            string outputPath = v2 ? Program.outputVersion2Path : Program.outputPath;
            string repositoryTarGz = v2 ? Program.repositoryVersion2TarGz : Program.repositoryTarGz;

            File.Delete(repositoryTemporaryTarGz);
            SimpleTarGz.CreateTar(repositoryTemporaryTarGz, outputPath);
            File.Delete(repositoryTarGz);
            File.Move(repositoryTemporaryTarGz, repositoryTarGz);
        }
    }
}
