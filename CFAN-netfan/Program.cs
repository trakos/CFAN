using System;
using System.IO;
using System.Linq;
using CFAN_netfan.CfanAggregator;
using CFAN_netfan.CfanAggregator.Aggregators;
using CFAN_netfan.CfanAggregator.Github;
using CFAN_netfan.CfanAggregator.ModFileNormalizer;
using CFAN_netfan.Compression;
using CKAN;
using CKAN.CmdLine;
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
        private static string repositoryTarGz => Path.Combine(repoPath, "repository.tar.gz");
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
            ModDirectoryManager fmmMirrorManager = new ModDirectoryManager(repoUrlPrefix, repoPath, "mods-fmm", modFileNormalizer, netFileCache);
            ModDirectoryManager githubModsDirectoryManager = new ModDirectoryManager(repoUrlPrefix, repoPath, "mods-github", modFileNormalizer, netFileCache);
            CombinedCfanAggregator combinedAggregator = new CombinedCfanAggregator(new ICfanAggregator[]
            {
                new LocalRepositoryAggregator(manualModDirectoryManager),
                new FactorioModsComAggregator(manualModDirectoryManager, fmmMirrorManager), 
                new GithubAggregator(githubModsDirectoryManager, new GithubRepositoriesDataProvider(), githubAccessToken),
            });
            combinedAggregator.getAllCfanJsons(user).ToList().ForEach(p => saveCfanJson(user, p));
            createFinalRepositoryTarGz(user);
            user.RaiseMessage("Done.");
        }

        protected static void saveCfanJson(IUser user, CfanJson cfanJson)
        {
            string subdirectory = cfanJson.modInfo.name;
            string filename = cfanJson.modInfo.name + "_" + cfanJson.modInfo.version + ".cfan";
            Directory.CreateDirectory(Path.Combine(outputPath, subdirectory));
            File.WriteAllText(
                Path.Combine(outputPath, subdirectory, filename),
                JsonConvert.SerializeObject(cfanJson)
            );
            user.RaiseMessage($"Generated {filename}");
        }

        protected static void createFinalRepositoryTarGz(IUser user)
        {
            File.Delete(repositoryTemporaryTarGz);
            SimpleTarGz.CreateTar(repositoryTemporaryTarGz, outputPath);
            File.Delete(repositoryTarGz);
            File.Move(repositoryTemporaryTarGz, repositoryTarGz);
        }
    }
}
