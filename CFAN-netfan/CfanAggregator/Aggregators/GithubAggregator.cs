using System;
using System.Collections.Generic;
using System.Linq;
using CFAN_netfan.CfanAggregator.Github;
using CKAN;
using CKAN.Factorio.Schema;
using Octokit;

namespace CFAN_netfan.CfanAggregator.Aggregators
{
    class GithubAggregator : ICfanAggregator
    {
        protected GithubRepositoriesDataProvider githubRepositoriesDataProvider;
        protected ModDirectoryManager modDirectoryManager;
        protected string accessToken;

        public GithubAggregator(ModDirectoryManager modDirectoryManager, GithubRepositoriesDataProvider githubRepositoriesDataProvider, string accessToken)
        {
            this.githubRepositoriesDataProvider = githubRepositoriesDataProvider;
            this.modDirectoryManager = modDirectoryManager;
            this.accessToken = accessToken;
        }

        public IEnumerable<CfanJson> getAllCfanJsons(IUser user)
        {
            var client = new GitHubClient(new ProductHeaderValue("CFAN", CKAN.Meta.ReleaseNumber()?.ToString()));
            if (!string.IsNullOrEmpty(accessToken))
            {
                client.Credentials = new Credentials(accessToken);
            }
            string[] allRepos = githubRepositoriesDataProvider.GithubRepositories;
            foreach (var repo in allRepos.Select(p => p.Split('/')).Select(p => new { owner = p[0], name = p[1]}))
            {
                var releases = client.Repository.Release.GetAll(repo.owner, repo.name).Result;
                int count = 0;
                foreach (Release release in releases)
                {
                    if (count > 0 && repo.name != "DyTech")
                    {
                        // return only newest one to speed up things
                        // DyTech has different mods in different releases, so exclude it from this shortcut
                        break;
                    }
                    if (!release.Assets.Any())
                    {
                        user.RaiseError($"No assets for {repo.owner}/{repo.name}:{release.TagName}");
                        continue;
                    }
                    if (release.Assets.Count > 1)
                    {
                        user.RaiseError($"Unexpected {release.Assets.Count} assets for {repo.owner}/{repo.name}:{release.TagName}");
                        continue;
                    }
                    ReleaseAsset asset = release.Assets[0];
                    string downloadedFilePath;
                    try
                    {
                        downloadedFilePath = modDirectoryManager.getCachedOrDownloadFile(user, asset.BrowserDownloadUrl,
                            $"{repo.name}-{release.TagName}");
                    }
                    catch (NetfanNormalizerKraken e)
                    {
                        user.RaiseError($"Couldn't normalize asset {repo.owner}/{repo.name}:{release.TagName}: {e.Message}");
                        continue;
                    }
                    catch (NetfanDownloadKraken e)
                    {
                        user.RaiseError($"Couldn't download {repo.owner}/{repo.name}:{release.TagName}: {e.Message}");
                        continue;
                    }
                    count++;
                    yield return modDirectoryManager.generateCfanFromZipFile(user, downloadedFilePath, new Dictionary<string, string>
                    {
                        ["x-source"] = typeof(FactorioModsComAggregator).Name,
                        ["github-repo"] = $"{repo.owner}/{repo.name}"
                    });
                }
            }
        }

        public void mergeCfanJson(IUser user, CfanJson destination, CfanJson source)
        {
            destination.aggregatorData["github-repo"] = source.aggregatorData["github-repo"];
        }

    }
}
