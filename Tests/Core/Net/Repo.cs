using CKAN;
using CKAN.Factorio;
using CKAN.Factorio.Version;
using NUnit.Framework;
using Tests.Data;

namespace Tests.Core.Net
{
    [TestFixture]
    public class Repo
    {
        private CKAN.Registry registry;
        private DisposableKSP ksp;

        [SetUp]
        public void Setup()
        {
            ksp = new DisposableKSP();
            registry = ksp.KSP.Registry;

            registry.ClearAvailable();
            registry.ClearPreexistingModules();
            registry.Installed().Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ksp.Dispose();
        }

        [Test]
        public void UpdateRegistryTarGz()
        {
            CKAN.Repo.UpdateRegistry(TestData.TestKANTarGz(), registry, ksp.KSP, new NullUser());

            // Test we've got an expected module.
            CfanModule far = registry.LatestAvailable("FARL", new FactorioVersion("0.25.0"));

            Assert.AreEqual("0.5.25", far.modVersion.ToString());
        }

        [Test]
        public void UpdateRegistryZip()
        {
            CKAN.Repo.UpdateRegistry(TestData.TestKANZip(), registry, ksp.KSP, new NullUser());

            // Test we've got an expected module.
            CfanModule far = registry.LatestAvailable("FARL", new FactorioVersion("0.25.0"));

            Assert.AreEqual("0.5.25", far.modVersion.ToString());
        }

        [Test]
        public void BadKanTarGz()
        {
            Assert.DoesNotThrow(delegate
            {
                CKAN.Repo.UpdateRegistry(TestData.BadKANTarGz(), registry, ksp.KSP, new NullUser());
            });
        }

        [Test]
        public void BadKanZip()
        {
            Assert.DoesNotThrow(delegate
                {
                    CKAN.Repo.UpdateRegistry(TestData.BadKANZip(), registry, ksp.KSP, new NullUser());
                });
        }
    }
}
