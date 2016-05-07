using CKAN.Factorio.Version;
using NUnit.Framework;

namespace Tests.Core.Types
{
    [TestFixture]
    public class KSPVersion
    {
        [Test]
        public void MinMax()
        {
            var min = new ModVersion("0.23");
            var max = new ModVersion("0.23");

            min = ModVersion.minWithTheSameMinor(min);
            max = ModVersion.maxWithTheSameMinor(max);

            Assert.IsTrue(min.ToString() == "0.23.0");
            Assert.IsTrue(max.ToString() == "0.23." + 0x7fffffff.ToString());

            Assert.IsTrue(min < max);
            Assert.IsTrue(max > min);
        }

        [Test]
        public void Strings()
        {
            var vshort = new ModVersion("0.23");
            var vlong = new ModVersion("0.23.5");
            
            Assert.AreEqual(vshort.ToString(), "0.23");
            Assert.AreEqual(vlong.ToString(), "0.23.5");
        }
    }
}