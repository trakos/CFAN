using CKAN;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;
using NUnit.Framework;
using Tests.Data;

namespace Tests.Core.Types
{
    [TestFixture]
    public class RelationshipDescriptor
    {

        AbstractVersion autodetected = new AutodetectedVersion("");

        [Test]
        [TestCase("asd==0.23", "asd", "0.23", true)]
        [TestCase("wibble", "wibble", "1.0", true)]
        [TestCase("asd==0.23", "asd", "0.23.1", false)]
        [TestCase("wibble", "wobble", "1.0", false)]
        public void VersionWithinBounds_ExactFalse(string modDependencyString, string modName, string versionName, bool expected)
        {
            var modDependency = new ModDependency(modDependencyString);
            Assert.AreEqual(expected, modDependency.isSatisfiedBy(modName, new ModVersion(versionName)));
        }

        [Test]
        [TestCase("0.20", "0.23", "0.21", true)]
        [TestCase("0.20", "0.23", "0.20", true)]
        [TestCase("0.20", "0.23", "0.23", true)]
        public void VersionWithinBounds_MinMax(string min, string max, string compareTo, bool expected)
        {
            string modName = "someModName";
            var modDependency = new ModDependency($"{modName}>={min}<={max}");
            var version = new ModVersion(compareTo);

            Assert.AreEqual(expected, modDependency.isSatisfiedBy(modName, version));
        }

        [Test]
        [TestCase("0.23")]
        public void VersionWithinBounds_vs_AutoDetectedMod(string version)
        {
            string modName = "someModName";
            var modDependency = new ModDependency($"{modName}>=5.6.0");
            var autodetected = new AutodetectedVersion("1.0");

            Assert.False(modDependency.isSatisfiedBy(modName, autodetected));
        }

        [Test]
        [TestCase("0.20", "0.23")]
        public void VersionWithinBounds_MinMax_vs_AutoDetectedMod(string min, string max)
        {
            string modName = "someModName";
            var modDependency = new ModDependency($"{modName}>={min}<={max}");
            var autodetected = new AutodetectedVersion("0.0.1");

            Assert.False(modDependency.isSatisfiedBy(modName, autodetected));
        }
    }
}

