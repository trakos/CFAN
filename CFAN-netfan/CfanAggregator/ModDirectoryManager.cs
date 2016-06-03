using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using CFAN_netfan.CfanAggregator.ModFileNormalizer;
using CKAN;
using CKAN.Factorio;
using CKAN.Factorio.Schema;
using CKAN.Factorio.Version;

namespace CFAN_netfan.CfanAggregator
{
    class ModDirectoryManager
    {
        protected string repoUrlPrefix;
        protected string repoLocalPath;
        protected string subDirectoryName;

        public string RepoModsDirectoryPath => Path.Combine(repoLocalPath, subDirectoryName);
        public string RepoPacksDirectoryPath => Path.Combine(repoLocalPath, subDirectoryName + "-packs");

        protected string RepoExternalUrl => repoUrlPrefix + "/" + subDirectoryName + "/";

        protected IModFileNormalizer modNormalizer;
        protected NetFileCache cache;

        public ModDirectoryManager(string repoUrlPrefix, string repoLocalPath, string subDirectoryName, IModFileNormalizer modNormalizer, NetFileCache netFileCache)
        {
            this.repoUrlPrefix = repoUrlPrefix;
            this.repoLocalPath = repoLocalPath;
            this.subDirectoryName = subDirectoryName;
            this.modNormalizer = modNormalizer;
            this.cache = netFileCache;
            Directory.CreateDirectory(RepoModsDirectoryPath);
        }

        public CfanJson generateCfanFromZipFile(IUser user, string file, Dictionary<string, string> aggregatorData)
        {
            if (Path.GetDirectoryName(file) != RepoModsDirectoryPath)
            {
                throw new Exception($"Unexpected file '{file}' not in mods directory!");
            }
            CfanJson cfanJson = CfanGenerator.createCfanJsonFromFile(file);
            cfanJson.aggregatorData = aggregatorData;
            cfanJson.downloadUrls = new string[] { RepoExternalUrl + Path.GetFileName(file) };
            return cfanJson;
        }

        public CfanJson generateCfanFromModPackJsonFile(IUser user, string file, Dictionary<string, string> aggregatorData)
        {
            if (Path.GetDirectoryName(file) != RepoPacksDirectoryPath)
            {
                throw new Exception($"Unexpected file '{file}' not in mods directory!");
            }
            if (Path.GetExtension(file) != ".json")
            {
                throw new Exception($"Unexpected file '{file}' in packs!");
            }
            string[] splitStrings = Path.GetFileNameWithoutExtension(file).Split(new[] { '-' }, 3);
            string author = splitStrings[0];
            string nameAndTitle = splitStrings[1];
            ModVersion version = new ModVersion(splitStrings[2]);
            string description = $"This is a meta-package that will install all mods from the modpack {nameAndTitle} by {author}.";
            CfanJson cfanJson = CfanGenerator.createCfanJsonFromModListJson(file, nameAndTitle, nameAndTitle, version, author, description);
            cfanJson.aggregatorData = aggregatorData;
            return cfanJson;
        }

        public string getCachedOrDownloadFile(IUser user, string url, string expectedFilename)
        {
            string cachePath = getPathToCachedOrDownloadedFile(user, url, expectedFilename);
            string firstCharacters = head(cachePath, 100).TrimStart();
            if (firstCharacters.StartsWith("<!DOCTYPE"))
            {
                throw new HtmlInsteadOfModDownloadedKraken("Downloaded some kind of html.");
            }
            // the json string with error usually is from github
            if (firstCharacters.StartsWith("{\"error\":"))
            {
                throw new HtmlInsteadOfModDownloadedKraken("Downloaded some kind of json error.");
            }
            string temporaryFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(cachePath, temporaryFile);
            modNormalizer.normalizeModFile(temporaryFile, Path.GetFileNameWithoutExtension(expectedFilename));
            // move to target place
            ModInfoJson modInfo = FactorioModParser.parseMod(temporaryFile);
            expectedFilename = CfanModule.createStandardFileName(modInfo.name, modInfo.version.ToString());
            // normalize again, this time with real filename (in order to fix directory name in zip file)
            modNormalizer.normalizeModFile(temporaryFile, expectedFilename);
            string fmmModFile = Path.Combine(RepoModsDirectoryPath, expectedFilename) + ".zip";
            if (File.Exists(fmmModFile))
            {
                File.Delete(fmmModFile);
            }
            File.Move(temporaryFile, fmmModFile);
            return fmmModFile;
        }

        const string DOWNLOAD_FILE_ERROR_TEXT = "placeholder for previous download failure";

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


        private string getPathToCachedOrDownloadedFile(IUser user, string url, string filenameCacheDescription)
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
