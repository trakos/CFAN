using System;
using System.IO;
using CKAN;
using NUnit.Framework;
using System.Net;
using Newtonsoft.Json.Linq;
using Tests.Data;

namespace Tests.Core.AutoUpdate
{
    [TestFixture]
    public class AutoUpdate
    {
        [Test]
        // We expect a kraken when looking at a json with no releases.
        public void FetchCkanUrl()
        {
            Assert.Throws<CKAN.Kraken>(delegate
            {
                string jsonText = File.ReadAllText(TestData.GithubEmptyAssetsJsonFilePath());
                JObject jObject = JObject.Parse(jsonText);
                CKAN.AutoUpdate.Instance.RetrieveUrl(jObject, "cfan.exe");
            }
            );
        }

        [Test]
        [Category("Online")]
        // This could fail if run during a release, so it's marked as Flaky.
        [Category("FlakyNetwork")]
        public void FetchLatestReleaseInfo()
        {
            var updater = CKAN.AutoUpdate.Instance;

            // Is is a *really* basic test to just make sure we get release info
            // if we ask for it.
            updater.FetchLatestReleaseInfo();
            Assert.IsTrue(updater.IsFetched());
            Assert.IsNotNull(updater.ReleaseNotes);
            Assert.IsNotNull(updater.LatestVersion);
        }

        [Test]
        [TestCase("aaa\r\n---\r\nbbb", "bbb", "Release note marker included")]
        [TestCase("aaa\r\nbbb", "aaa\r\nbbb", "No release note marker")]
        [TestCase("aaa\r\n---\r\nbbb\r\n---\r\nccc", "bbb\r\n---\r\nccc", "Multi release notes markers")]
        public void ExtractReleaseNotes(string body, string expected, string comment)
        {
            Assert.AreEqual(
                expected,
                CKAN.AutoUpdate.Instance.ExtractReleaseNotes(body),
                comment
            );
        }
    }
}