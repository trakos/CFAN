using System.Collections.Generic;
using System.Linq;
using CKAN;
using CKAN.Factorio;
using NUnit.Framework;
using Tests.Data;

// We're exercising FindReverseDependencies in here, because:
// - We need a registry
// - It calls the sanity checker code to do the heavy lifting.

namespace Tests.Core.Relationships
{
    [TestFixture]
    public class SanityChecker
    {
        private CKAN.Registry registry;
        private DisposableKSP ksp;

        [TestFixtureSetUp]
        public void Setup()
        {
            ksp = new DisposableKSP();

            registry = ksp.KSP.Registry;
            registry.ClearAvailable();
            registry.ClearPreexistingModules();
            registry.Installed().Clear();

            Repo.UpdateRegistry(TestData.TestKANTarGz(), registry, ksp.KSP, new NullUser());
        }

        [Test]
        public void Empty()
        {
            Assert.IsTrue(CKAN.SanityChecker.IsConsistent(new List<CfanModule>()));
        }

        [Test]
        public void Void()
        {
            Assert.IsTrue(CKAN.SanityChecker.IsConsistent(null));
        }

        [Test]
        public void DogeCoin()
        {
            // Test with a module that depends and conflicts with nothing.
            var mods = new List<CfanModule> {registry.LatestAvailable("PersonalRoboportFix", null)};

            Assert.IsTrue(CKAN.SanityChecker.IsConsistent(mods), "PersonalRoboportFix");
        }

        [Test]
        public void CustomBiomes()
        {
            var mods = new List<CfanModule> {registry.LatestAvailable("5dim_ores", null)};

            Assert.IsFalse(CKAN.SanityChecker.IsConsistent(mods), "5dim_ores without boblibrary");

            mods.Add(registry.LatestAvailable("boblibrary", null));
            Assert.IsFalse(CKAN.SanityChecker.IsConsistent(mods), "5dim_ores with boblibrary");

            mods.Add(registry.LatestAvailable("5dim_core", null));
            Assert.IsTrue(CKAN.SanityChecker.IsConsistent(mods), "5dim_ores with boblibrary and 5dim_core");
        }

        [Test]
        public void CustomBiomesWithDlls()
        {
            var mods = new List<CfanModule>();
            var dlls = new List<string> { "5dim_ores" };

            Assert.IsTrue(CKAN.SanityChecker.IsConsistent(mods, dlls), "5dim_ores without boblibrary");

            // This would actually be a terrible thing for users to have, but it tests the
            // relationship we want.
            mods.Add(registry.LatestAvailable("boblibrary", null));
            Assert.IsTrue(CKAN.SanityChecker.IsConsistent(mods, dlls), "5dim_ores with boblibrary");

            mods.Add(registry.LatestAvailable("5dim_core", null));
            Assert.IsTrue(CKAN.SanityChecker.IsConsistent(mods, dlls), "5dim_ores with boblibrary and 5dim_core");
        }

        /*[Test]
        public void ConflictWithDll()
        {
            var mods = new List<CfanModule> { registry.LatestAvailable("SRL",null) };
            var dlls = new List<string> { "QuickRevert" };

            Assert.IsTrue(CKAN.SanityChecker.IsConsistent(mods), "SRL can be installed by itself");
            Assert.IsFalse(CKAN.SanityChecker.IsConsistent(mods, dlls), "SRL conflicts with QuickRevert DLL");
        }

        [Test]
        public void ModulesToProvides()
        {
            var mods = new List<CfanModule>
            {
                registry.LatestAvailable("CustomBiomes",null),
                registry.LatestAvailable("CustomBiomesKerbal",null),
                registry.LatestAvailable("DogeCoinFlag",null)
            };

            var provides = CKAN.SanityChecker.ModulesToProvides(mods);
            Assert.Contains("CustomBiomes", provides.Keys);
            Assert.Contains("CustomBiomesData", provides.Keys);
            Assert.Contains("CustomBiomesKerbal", provides.Keys);
            Assert.Contains("DogeCoinFlag", provides.Keys);
            Assert.AreEqual(4, provides.Keys.Count);
        }*/

        [Test]
        public void FindUnmetDependencies()
        {
            var mods = new List<CfanModule>();
            var dlls = Enumerable.Empty<string>();
            Assert.IsEmpty(CKAN.SanityChecker.FindUnmetDependencies(mods, dlls), "Empty list");

            mods.Add(registry.LatestAvailable("PersonalRoboportFix", null));
            Assert.IsEmpty(CKAN.SanityChecker.FindUnmetDependencies(mods, dlls), "PersonalRoboportFix");

            mods.Add(registry.LatestAvailable("5dim_ores", null));
            Assert.Contains("boblibrary", CKAN.SanityChecker.FindUnmetDependencies(mods, dlls).Keys, "Missing boblibrary");
            Assert.Contains("5dim_core", CKAN.SanityChecker.FindUnmetDependencies(mods, dlls).Keys, "Missing 5dim_core");

            mods.Add(registry.LatestAvailable("5dim_core", null));
            mods.Add(registry.LatestAvailable("boblibrary", null));
            Assert.IsEmpty(CKAN.SanityChecker.FindUnmetDependencies(mods, dlls));

            mods.RemoveAll(x => x.identifier == "boblibrary");
            Assert.AreEqual(3, mods.Count, "Checking removed boblibrary");

            Assert.Contains("boblibrary", CKAN.SanityChecker.FindUnmetDependencies(mods, dlls).Keys, "Missing boblibrary");
        }

        [Test]
        public void ReverseDepends()
        {
            var mods = new List<CfanModule>
            {
                registry.LatestAvailable("bobores",null),
                registry.LatestAvailable("boblibrary",null),
                registry.LatestAvailable("PersonalRoboportFix",null)
            };

            // Make sure some of our expectations regarding dependencies are correct.
            Assert.Contains("boblibrary", registry.LatestAvailable("bobores", null).depends.Select(x => x.modName).ToList());

            // Removing PRF should only remove itself.
            var to_remove = new List<string> { "PersonalRoboportFix" };
            TestDepends(to_remove, mods, null, to_remove, "PersonalRoboportFix Removal");

            // Removing CB should remove its data/*, and vice-versa.*/
            to_remove.Clear();
            to_remove.Add("boblibrary");
            var expected = new List<string> { "bobores", "boblibrary" };
            TestDepends(to_remove, mods, null, expected, "boblibrary removed");

            /*// We expect the same result removing CBK
            to_remove.Clear();
            to_remove.Add("CustomBiomesKerbal");
            TestDepends(to_remove, mods, null, expected, "CustomBiomesKerbal removed");*/

            // And we expect the same result if we try to remove both.
            to_remove.Add("bobores");
            TestDepends(to_remove, mods, null, expected, "bobores and boblibrary removed");

            // Finally, if we try to remove nothing, we shold get back the empty set.
            expected.Clear();
            to_remove.Clear();
            TestDepends(to_remove, mods, null, expected, "Removing nothing");

        }

        private static void TestDepends(List<string> to_remove, List<CfanModule> mods, List<string> dlls, List<string> expected, string message)
        {
            dlls = dlls ?? new List<string>();

            var remove_count = to_remove.Count;
            var dll_count = dlls.Count;
            var mods_count = mods.Count;

            var results = CKAN.Registry.FindReverseDependencies(to_remove, mods, dlls);

            // Make sure nothing changed.
            Assert.AreEqual(remove_count, to_remove.Count, message + " remove count");
            Assert.AreEqual(dll_count, dlls.Count, message + " dll count");
            Assert.AreEqual(mods_count, mods.Count, message + " mods count");

            // Check our actual results.
            CollectionAssert.AreEquivalent(expected, results, message);
        }
    }
}

