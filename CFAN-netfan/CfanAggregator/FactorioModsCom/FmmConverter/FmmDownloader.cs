using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CFAN_netfan.CfanAggregator.Aggregators;
using CFAN_netfan.CfanAggregator.FactorioModsCom.Schema;
using CKAN;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom.FmmConverter
{
    class FmmDownloader : IFmmConverter
    {
        protected ModDirectoryManager fmmManager;

        public FmmDownloader(ModDirectoryManager fmmManager)
        {
            this.fmmManager = fmmManager;
        }

        public IEnumerable<CfanJson> generateCfanJsons(IUser user, ModJson modJson)
        {
            CfanJson[] cfanJsons = yieldCfanJsons(user, modJson).ToArray();
            var groupedByNameAndVersions = cfanJsons.GroupBy(p => new {p.modInfo.version, p.modInfo.name}).ToArray();
            if (groupedByNameAndVersions.Any(p => p.Count() > 1))
            {
                user.RaiseError($"Some releases/files have duplicated name/version for {modJson.name}.");
                cfanJsons = groupedByNameAndVersions.Select(p => p.First()).ToArray();
            }
            return cfanJsons;
        }

        protected IEnumerable<CfanJson> yieldCfanJsons(IUser user, ModJson modJson)
        {
            foreach (ModReleaseJson modReleaseJson in modJson.releases)
            {
                foreach (ModReleaseJson.ModReleaseJsonFile modReleaseJsonFile in modReleaseJson.files)
                {
                    string url = checkUrl(user, modReleaseJsonFile.mirror, $"mod: {modJson.name}");
                    if (string.IsNullOrEmpty(url))
                    {
                        url = checkUrl(user, modReleaseJsonFile.url, $"mod: {modJson.name}");
                    }
                    if (string.IsNullOrEmpty(url))
                    {
                        user.RaiseError($"Mod {modJson.name} does not have download url, omitting");
                        continue;
                    }
                    string expectedFilename = modJson.name + "_" + modReleaseJson.version + ".zip";
                    string downloadedFilePath;
                    try
                    {
                        downloadedFilePath = fmmManager.getCachedOrDownloadFile(user, url, expectedFilename);
                    }
                    catch (NetfanDownloadKraken e)
                    {
                        user.RaiseError($"Couldn't download {modJson.name}: {e.Message}");
                        continue;
                    }
                    catch (Exception e)
                    {
                        user.RaiseError($"Couldn't handle {modJson.name}: {e.Message}");
                        continue;
                    }
                    yield return fmmManager.generateCfanFromZipFile(user, downloadedFilePath, new Dictionary<string, string>
                    {
                        ["x-source"] = typeof(FactorioModsComAggregator).Name,
                        ["fmm-id"] = modJson.id.ToString()
                    });
                }
            }
        }

        protected string checkUrl(IUser user, string url, string context)
        {
            if (url == null)
            {
                return null;
            }
            // this seems to be some kind of placeholder url for empty link, I don't get it
            if (url.StartsWith("http:" + "//google.com"))
            {
                user.RaiseError($"Ignoring google.com link ({context})");
                return null;
            }
            if (url.Contains("yadi.sk"))
            {
                user.RaiseError($"Ignoring yandex link ({context})");
                return null;
            }
            if (url.Contains("mediafire.com"))
            {
                user.RaiseError($"Ignoring mediafire link ({context})");
                return null;
            }
            if (url.Contains("cloud.directupload.net"))
            {
                user.RaiseError($"Ignoring directupload link ({context})");
                return null;
            }
            // turn google drive link into link straight to the download
            string googleDriveLink = "https:" + "//drive.google.com/open?id=";
            if (url.StartsWith(googleDriveLink))
            {
                string fileId = url.Substring(googleDriveLink.Length);
                return "https:" + $"//docs.google.com/uc?export=download&id={fileId}";
            }
            var match = Regex.Match(url, "^https?://drive.google.com/file/d/(?<fileId>[a-zA-Z0-9_-]+)/");
            if (match.Success)
            {
                string fileId = match.Groups["fileId"].Value;
                return "https:" + $"//docs.google.com/uc?export=download&id={fileId}";
            }
            // dropbox links ending with ?dl=0 won't download (it will download an html)
            if (url.Contains("dropbox.com") && url.EndsWith("?dl=0"))
            {
                return url.Replace("?dl=0", "?dl=1");
            }
            return url;
        }
    }
}
