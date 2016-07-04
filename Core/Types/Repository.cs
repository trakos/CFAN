using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CKAN
{
    public class Repository
    {
        [JsonIgnore] public static readonly string default_ckan_repo_name = "default";
        [JsonIgnore] public static readonly Uri default_ckan_repo_uri = new Uri("http://cfan.trakos.pl/repo/repository.tar.gz");
        [JsonIgnore] public static readonly Uri default_ckan_repo_version2_uri = new Uri("http://cfan.trakos.pl/repo/repository_v2.tar.gz");
        [JsonIgnore] public static readonly Uri default_repo_master_list = new Uri("http://cfan.trakos.pl/repositories.json");

        public string name;
        public Uri uri;
        public int priority = 0;
        public Boolean ckan_mirror = false;

        public Repository()
        {
        }

        public Repository(string name, string uri) : this (name, new Uri(uri))
        {
        }

        public Repository(string name, string uri, int priority) : this(name, uri)
        {
            this.priority = priority;
        }

        public Repository(string name, Uri uri) : this()
        {
            this.name = name;
            // this version supports repo v2 (it includes cfan modules that requires factorio auth that might not be available for this version)
            if (uri.ToString() == default_ckan_repo_uri.ToString())
            {
                uri = default_ckan_repo_version2_uri;
            }
            this.uri = uri;
        }

        [OnDeserialized]
        private void DeSerialisationFixes(StreamingContext context)
        {
            // this version supports repo v2 (it includes cfan modules that requires factorio auth that might not be available for this version)
            if (uri.ToString() == default_ckan_repo_uri.ToString())
            {
                uri = default_ckan_repo_version2_uri;
            }
        }

        public override string ToString()
        {
            return String.Format("{0} ({1}, {2})", name, priority, uri);
        }
    }

}
