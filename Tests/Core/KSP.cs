using System;
using System.IO;
using CKAN;
using CKAN.Factorio.Version;
using NUnit.Framework;
using Tests.Data;

namespace Tests.Core
{
    [TestFixture]
    public class KSP
    {
        private CKAN.KSP ksp;
        private string ksp_dir;

        [SetUp]
        public void Setup()
        {
            ksp_dir = TestData.NewTempDir();
            TestData.CopyDirectory(TestData.good_factorio_dir(), ksp_dir);
            ksp = new CKAN.KSP(ksp_dir,NullUser.User);
        }

        [TearDown]
        public void TearDown()
        {
            ksp.Dispose();
            ksp = null;
            Directory.Delete(ksp_dir, true);
        }

        [Test]
        public void IsGameDir()
        {
            // Our test data directory should be good.
            Assert.IsTrue(CKAN.KSP.IsFactorioDirectory(TestData.good_factorio_dir()));

            // As should our copied folder.
            Assert.IsTrue(CKAN.KSP.IsFactorioDirectory(ksp_dir));

            // And the one from our KSP instance.
            Assert.IsTrue(CKAN.KSP.IsFactorioDirectory(ksp.GameDir()));

            // All these ones should be bad.
            foreach (string dir in TestData.bad_ksp_dirs())
            {
                Assert.IsFalse(CKAN.KSP.IsFactorioDirectory(dir));
            }
        }

        [Test]
        public void Scenarios()
        {
            //Use Uri to avoid issues with windows vs linux line seperators.
            var canonicalPath = new Uri(Path.Combine(ksp_dir, "scenario")).LocalPath;
            var training = new Uri(ksp.Scenarios()).LocalPath;
            Assert.AreEqual(canonicalPath, training);
        }

        [Test]
        public void ScanDlls()
        {
            string pathDir = Path.Combine(ksp.Mods(), "Example-0.1.0");
            Directory.CreateDirectory(pathDir);
            string infoJsonDir = Path.Combine(pathDir, "info.json");

            Assert.IsFalse(ksp.Registry.IsInstalled("Example"), "Example should start uninstalled");

            File.WriteAllText(infoJsonDir, @"{""name"":""Example"",""version"":""0.1.0"",""title"":""SomeTitle"",""author"":""someAuthor"",""contact"":""contact@example.com"",""homepage"":""http://example.com"",""description"":""someDescription"",""dependencies"":[]}");

            ksp.ScanGameData();

            Assert.IsTrue(ksp.Registry.IsInstalled("Example"), "Example installed");

            AbstractVersion version = ksp.Registry.InstalledVersion("Example");
            Assert.IsInstanceOf<AutodetectedVersion>(version, "DLL detected as a DLL, not full mod");

            // Now let's do the same with different case.
            pathDir = Path.Combine(ksp.Mods(), "NewMod-0.1.0");
            Directory.CreateDirectory(pathDir);
            infoJsonDir = Path.Combine(pathDir, "info.json");
            File.WriteAllText(infoJsonDir, @"{""name"":""NewMod"",""version"":""0.1.0"",""title"":""SomeTitle"",""author"":""someAuthor"",""contact"":""contact@example.com"",""homepage"":""http://example.com"",""description"":""someDescription"",""dependencies"":[]}");

            Assert.IsFalse(ksp.Registry.IsInstalled("NewMod"));

            ksp.ScanGameData();

            Assert.IsTrue(ksp.Registry.IsInstalled("NewMod"));
        }

        [Test]
        public void ToAbsolute()
        {
            Assert.AreEqual(
                CKAN.KSPPathUtils.NormalizePath(
                    Path.Combine(ksp_dir, "GameData/HydrazinePrincess")
                ),
                ksp.ToAbsoluteGameDataDir("GameData/HydrazinePrincess")
            );
        }

        [Test]
        public void ToRelative()
        {
            string absolute = Path.Combine(ksp_dir, "GameData/HydrazinePrincess");

            Assert.AreEqual(
                "GameData/HydrazinePrincess",
                ksp.ToRelativeGameDir(absolute)
            );
        }

    }
}