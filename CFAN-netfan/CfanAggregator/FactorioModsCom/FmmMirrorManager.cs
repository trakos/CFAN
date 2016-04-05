using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using CFAN_netfan.CfanAggregator.FactorioModsCom.ModFileNormalizer;
using CKAN;
using CKAN.Factorio;
using CKAN.Factorio.Schema;

namespace CFAN_netfan.CfanAggregator.FactorioModsCom
{
    class FmmMirrorManager
    {
        protected string repoUrlPrefix;
        protected string repoLocalPath;
        protected string repoFmmModsPath => Path.Combine(repoLocalPath, "fmm-mods");
        protected string cacheFmmModsPath => Path.Combine(repoLocalPath, "fmm-cache");
        protected string repoFmmModsUrl => repoUrlPrefix + "/fmm-mods/";
        protected CombinedModFileNormalizer modNormalizer;
        protected NetFileCache cache;

        public FmmMirrorManager(string repoUrlPrefix, string repoLocalPath, CombinedModFileNormalizer modNormalizer)
        {
            this.repoUrlPrefix = repoUrlPrefix;
            this.repoLocalPath = repoLocalPath;
            this.modNormalizer = modNormalizer;
            Directory.CreateDirectory(cacheFmmModsPath);
            Directory.CreateDirectory(repoFmmModsPath);
            cache = new NetFileCache(cacheFmmModsPath);
        }

        public CfanJson generateCfanFromZipFile(IUser user, string file)
        {
            CfanJson cfanJson = CfanGenerator.createCfanJsonFromFile(file);
            cfanJson.aggregatorData = new Dictionary<string, string>
            {
                ["x-source"] = typeof(FactorioModsComAggregator).Name
            };
            cfanJson.downloadUrls = new string[] { repoFmmModsUrl + Path.GetFileName(file) };
            return cfanJson;
        }

        public string getCachedOrDownloadFmmFile(IUser user, string url, string expectedFilename)
        {
            string cachePath = downloadOrGetCachedFile(user, url, expectedFilename);
            string firstCharacters = head(cachePath, 100).TrimStart();
            // the json string with error usually is from github
            if (firstCharacters.StartsWith("<!DOCTYPE") || firstCharacters.StartsWith("{\"error\":"))
            {
                throw new HtmlInsteadOfModDownloadedKraken("Downloaded some kind of html.");
            }
            string temporaryFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(cachePath, temporaryFile);
            modNormalizer.normalizeModFile(temporaryFile, Path.GetFileNameWithoutExtension(expectedFilename));
            // move to target place
            ModInfoJson modInfo = FactorioModParser.parseMod(temporaryFile);
            expectedFilename = CfanModule.createStandardFileName(modInfo.name, modInfo.version.ToString());
            string fmmModFile = Path.Combine(repoFmmModsPath, expectedFilename) + ".zip";
            if (File.Exists(fmmModFile))
            {
                File.Delete(fmmModFile);
            }
            File.Move(temporaryFile, fmmModFile);
            return fmmModFile;
        }

        private string head(string path, int n)
        {
            char[] c = new char[n + 1];
            using (StreamReader streamReader = File.OpenText(path))
            {
                streamReader.Read(c, 0, n);
            }
            c[n] = '\0';
            return new string(c);
        }

        const string DOWNLOAD_FILE_ERROR_TEXT = "placeholder for previous download failure";

        private string downloadOrGetCachedFile(IUser user, string url, string filenameCacheDescription)
        {
            string fullPath = cache.GetCachedFilename(new Uri(url));
            if (fullPath != null)
            {
                if (isPlaceholderErrorFile(fullPath))
                {
                    throw new CachedPreviousDownloadErrorKraken("File omitted because of previous error.");
                }
                user.RaiseMessage("Using {0} (cached)", filenameCacheDescription);
                return fullPath;
            }
            string tmpFile;
            try
            {
                tmpFile = Net.Download(url, null, user);
            }
            catch (WebException e)
            {
                HttpWebResponse response = e.Response as HttpWebResponse;
                if (response != null)
                {
                    HttpWebResponse webResponse = response;
                    tmpFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    File.WriteAllText(tmpFile, DOWNLOAD_FILE_ERROR_TEXT, Encoding.ASCII);
                    cache.Store(new Uri(url), tmpFile, filenameCacheDescription + "." + ((int)webResponse.StatusCode).ToString(), true);
                    throw new CachedPreviousDownloadErrorKraken(e.Message);
                }
                throw;
            }
            return cache.Store(new Uri(url), tmpFile, filenameCacheDescription, true);
        }

        private bool isPlaceholderErrorFile(string path)
        {
            return new FileInfo(path).Length == Encoding.ASCII.GetByteCount(DOWNLOAD_FILE_ERROR_TEXT) && File.ReadAllText(path) == DOWNLOAD_FILE_ERROR_TEXT;
        }
    }
}
