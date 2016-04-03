using System;
using System.IO;
using System.Linq;
using CFAN_netfan.CfanAggregator;
using CFAN_netfan.CfanAggregator.FactorioModsCom;
using CFAN_netfan.CfanAggregator.FactorioModsCom.ModFileNormalizer;
using CFAN_netfan.CfanAggregator.LocalRepository;
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
        private static string outputPath => Path.Combine(repoPath, "cfans");
        private static string repositoryTarGz => Path.Combine(repoPath, "repository.tar.gz");
        private static string repositoryTemporaryTarGz => Path.Combine(repoPath, "repository_tmp.tar.gz");


        [STAThread]
        static void Main(string[] args)
        {
            IUser user = new ConsoleUser(false);
            repoPath = args[0];
            repoUrlPrefix = args[1];
            CombinedModFileNormalizer modFileNormalizer = new CombinedModFileNormalizer(new IModFileNormalizer[]
            {
                new RarToZipNormalizer(), 
                new ModZipRootNormalizer()
            });
            LocalRepositoryManager localRepositoryManager = new LocalRepositoryManager(repoUrlPrefix, repoPath);
            FmmMirrorManager fmmMirrorManager = new FmmMirrorManager(repoUrlPrefix, repoPath, modFileNormalizer);
            CombinedCfanAggregator combinedAggregator = new CombinedCfanAggregator(new ICfanAggregator[]
            {
                new LocalRepositoryAggregator(localRepositoryManager),
                new FactorioModsComAggregator(localRepositoryManager, fmmMirrorManager), 
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
