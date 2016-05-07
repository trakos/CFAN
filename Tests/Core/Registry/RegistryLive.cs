using CKAN;
using CKAN.Factorio;
using NUnit.Framework;
using Tests.Data;

namespace Tests.Core.Registry
{
    /// <summary>
    /// These are tests on a live registry extracted from one of the developers'
    /// systems.
    /// </summary>

    [TestFixture]
    public class RegistryLive
    {
        private static string test_registry = TestData.TestRegistry();
        private DisposableKSP temp_ksp;
        private CKAN.IRegistryQuerier registry;

        [SetUp]
        public void Setup()
        {
            // Make a fake KSP install
            temp_ksp = new DisposableKSP(null, test_registry);

            // Easy short-cut
            registry = temp_ksp.KSP.Registry;
        }

        [TearDown]
        public void TearDown()
        {
            temp_ksp.Dispose();
        }

        [Test]
        public void LatestAvailable()
        {
            CfanModule module =
                registry.LatestAvailable("FARL", temp_ksp.KSP.Version());

            Assert.AreEqual("FARL", module.identifier);
            Assert.AreEqual("0.5.24", module.modVersion.ToString());
        }
    }
}

