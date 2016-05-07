using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using CKAN;
using CKAN.Factorio;
using CKAN.Installable;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using Tests.Data;

namespace Tests.Core
{
    [TestFixture]
    public class ModuleInstaller
    {
        private string dogezip;
        private CfanModule dogemod;

        private string mm_zip;
        private CfanModule mm_mod;

        private DisposableKSP ksp;

        [SetUp]
        public void Setup()
        {
            // By setting these for every test, we can make sure our tests can change
            // them any way they like without harming other tests.
            
            dogezip = TestData.DogeCoinFlagZip();
            dogemod = TestData.DogeCoinFlag_101_module();

            mm_zip = TestData.ModuleManagerZip();
            mm_mod = TestData.ModuleManagerModule();

            ksp = new DisposableKSP();
        }

        [TearDown]
        public void TearDown()
        {
            ksp.Dispose();
        }

        // Test data: different ways to install the same file.
        public static CfanModule[] doge_mods =
        {
            TestData.DogeCoinFlag_101_module()
        };

        // GH #315, all of these should result in the same output.
        // Even though they're not necessarily all spec-valid, we should accept them
        // nonetheless.
        public static readonly string[] SuchPaths =
        {
            "GameData/SuchTest",
            "GameData/SuchTest/",
            "GameData\\SuchTest",
            "GameData\\SuchTest\\",
            "GameData\\SuchTest/",
            "GameData/SuchTest\\"
        };

        [Test]
        public void ModuleManagerInstall()
        {
            using (var tidy = new DisposableKSP())
            {
                List<IInstallable> contents = CKAN.ModuleInstaller.FindInstallableFiles(mm_mod, mm_zip, tidy.KSP);

                Assert.True(contents.Any(x => x.Destination == "FARL_0.2.5.zip"));
            }
        }

#pragma warning disable 0414

        // All of these targets should fail.
        public static readonly string[] BadTargets = {
            "GameDataIsTheBestData", "Shups", "GameData/../../../../etc/pwned",
            "Ships/Foo", "GameRoot/saves", "GameRoot/CKAN", "GameData/..",
            @"GameData\..\..\etc\pwned", @"GameData\.."
        };

#pragma warning restore 0414

        [Test]
        public void UninstallModNotFound()
        {
            KSPManager manager = new KSPManager(new NullUser(), new FakeWin32Registry(ksp.KSP)){CurrentInstance = ksp.KSP};

            Assert.Throws<ModNotInstalledKraken>(delegate
            {
                // This should throw, as our tidy KSP has no mods installed.
                CKAN.ModuleInstaller.GetInstance(manager.CurrentInstance, NullUser.User).UninstallList("Foo");
            });

            manager.Dispose();
        }

        [Test]
        public void CanInstallMod()
        {
            string mod_file_name = "DoubleFurnace_0.1.2.zip";
            
            // Make sure the mod is not installed.
            string mod_file_path = Path.Combine(ksp.KSP.Mods(), mod_file_name);

            Assert.IsFalse(File.Exists(mod_file_path));

            // Copy the zip file to the cache directory.
            Assert.IsFalse(ksp.KSP.Cache.IsCachedZip(TestData.DogeCoinFlag_101_module().download));

            string cache_path = ksp.KSP.Cache.Store(TestData.DogeCoinFlag_101_module().download, TestData.DogeCoinFlagZip());

            Assert.IsTrue(ksp.KSP.Cache.IsCachedZip(TestData.DogeCoinFlag_101_module().download));
            Assert.IsTrue(File.Exists(cache_path));

            // Mark it as available in the registry.
            Assert.AreEqual(0, ksp.KSP.Registry.Available(ksp.KSP.Version()).Count());

            ksp.KSP.Registry.AddAvailable(TestData.DogeCoinFlag_101_module());

            Assert.AreEqual(1, ksp.KSP.Registry.Available(ksp.KSP.Version()).Count());

            // Attempt to install it.
            List<CfanModuleIdAndVersion> modules = new List<CfanModuleIdAndVersion> {new CfanModuleIdAndVersion(TestData.DogeCoinFlag_101_module().identifier)};

            CKAN.ModuleInstaller.GetInstance(ksp.KSP, NullUser.User).InstallList(modules, new RelationshipResolverOptions());

            // Check that the module is installed.
            Assert.IsTrue(File.Exists(mod_file_path));
        }

        [Test]
        public void CanUninstallMod()
        {
            string mod_file_name = "DoubleFurnace_0.1.2.zip";

            using (
                KSPManager manager = new KSPManager(new NullUser(), new FakeWin32Registry(ksp.KSP))
                {
                    CurrentInstance = ksp.KSP
                })
            {
                Debug.WriteLine(ksp.KSP.DownloadCacheDir());
                Console.WriteLine(ksp.KSP.DownloadCacheDir());

                Assert.IsTrue(Directory.Exists(ksp.KSP.DownloadCacheDir()));

                string mod_file_path = Path.Combine(ksp.KSP.Mods(), mod_file_name);

                // Install the test mod.
                ksp.KSP.Cache.Store(TestData.DogeCoinFlag_101_module().download, TestData.DogeCoinFlagZip());
                ksp.KSP.Registry.AddAvailable(TestData.DogeCoinFlag_101_module());
                
                List<CfanModuleIdAndVersion> modules = new List<CfanModuleIdAndVersion> { new CfanModuleIdAndVersion(TestData.DogeCoinFlag_101_module().identifier) };

                CKAN.ModuleInstaller.GetInstance(manager.CurrentInstance, NullUser.User).InstallList(modules, new RelationshipResolverOptions());

                // Check that the module is installed.
                Assert.IsTrue(File.Exists(mod_file_path));

                // Attempt to uninstall it.
                CKAN.ModuleInstaller.GetInstance(manager.CurrentInstance, NullUser.User).UninstallList(modules.Select(p => p.identifier));

                // Check that the module is not installed.
                Assert.IsFalse(File.Exists(mod_file_path));
            }
        }

        [Test]
        public void ModuleManagerInstancesAreDecoupled()
        {
            string mod_file_name = "DoubleFurnace_0.1.2.zip";

            // Create a new disposable KSP instance to run the test on.
            Assert.DoesNotThrow(delegate
            {
                for (int i = 0; i < 5; i++)
                {
                    using (DisposableKSP ksp = new DisposableKSP())
                    {
                        // Copy the zip file to the cache directory.
                        ksp.KSP.Cache.Store(TestData.DogeCoinFlag_101_module().download, TestData.DogeCoinFlagZip());

                        // Mark it as available in the registry.
                        ksp.KSP.Registry.AddAvailable(TestData.DogeCoinFlag_101_module());

                        // Attempt to install it.
                        List<CfanModuleIdAndVersion> modules = new List<CfanModuleIdAndVersion> { new CfanModuleIdAndVersion(TestData.DogeCoinFlag_101_module().identifier) };

                        CKAN.ModuleInstaller.GetInstance(ksp.KSP, NullUser.User).InstallList(modules, new RelationshipResolverOptions());

                        // Check that the module is installed.
                        string mod_file_path = Path.Combine(ksp.KSP.Mods(), mod_file_name);

                        Assert.IsTrue(File.Exists(mod_file_path));
                    }
                }
            }
            );
        }

    }
}

