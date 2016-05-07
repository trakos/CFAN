using CKAN;
using CKAN.Factorio.Version;
using NUnit.Framework;

namespace Tests.Core.Types
{
    [TestFixture]
    public class Version
    {
        [Test]
        [ExpectedException(typeof(BadVersionKraken))]
        public void Alpha()
        {
            var v1 = new ModVersion("apple");
            var v2 = new ModVersion("banana");
        }

        [Test]
        public void Basic()
        {
            var v0 = new ModVersion("1.2.0");
            var v1 = new ModVersion("1.2.0");
            var v2 = new ModVersion("1.2.1");

            Assert.That(v1.IsLessThan(v2));
            Assert.That(v2.IsGreaterThan(v1));
            Assert.That(v1.IsEqualTo(v0));
        }

        [Test]
        public void Issue1076()
        {
            var v0 = new ModVersion("1.01");
            var v1 = new ModVersion("1.1");

            Assert.That(v1.IsEqualTo(v0));
        }

        [Test]
        [ExpectedException(typeof(BadVersionKraken))]
        public void SortAllNonNumbersBeforeDot()
        {
            var v0 = new ModVersion("1.0_beta");
            var v1 = new ModVersion("1.0.1_beta");
        }

        [Test]
        [ExpectedException(typeof(BadVersionKraken))]
        public void DotSeparatorForExtraData()
        {
            var v0 = new ModVersion("1.0");
            var v1 = new ModVersion("1.0.repackaged");
            var v2 = new ModVersion("1.0.1");
        }

        [Test]
        public void UnevenVersioning()
        {
            var v0 = new ModVersion("1.1.0.0");
            var v1 = new ModVersion("1.1.1");

            Assert.That(v0.IsLessThan(v1));
            Assert.That(v1.IsGreaterThan(v0));
        }

        [Test]
        public void DllVersion()
        {
            var v1 = new AutodetectedVersion("2.4");
            Assert.AreEqual("2.4", v1.ToString());
        }

        [Test]
        public void ProvidesVersion()
        {
            var v1 = new ProvidedVersion("SomeModule", "5.0");
            Assert.AreEqual("5.0", v1.ToString());
        }
    }
}