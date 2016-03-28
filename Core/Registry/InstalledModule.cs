using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using CKAN.Factorio;
using CKAN.Factorio.Relationships;
using CKAN.Factorio.Schema;
using Newtonsoft.Json;

namespace CKAN
{
    [JsonObject(MemberSerialization.OptIn)]
    public class InstalledModuleFile
    {

        // TODO: This class should also record file paths as well.
        // It's just sha1 now for registry compatibility.

        [JsonProperty] private string sha1_sum;

        public string Sha1
        {
            get
            {
                return sha1_sum;
            }
        }

        public InstalledModuleFile(string path, KSP ksp)
        {
            string absolute_path = ksp.ToAbsoluteGameDataDir(path);
            sha1_sum = Sha1Sum(absolute_path);
        }

        // We need this because otherwise JSON.net tries to pass in
        // our sha1's as paths, and things go wrong.
        [JsonConstructor]
        private InstalledModuleFile()
        {
        }

        /// <summary>
        /// Returns the sha1 sum of the given filename.
        /// Returns null if passed a directory.
        /// Throws an exception on failure to access the file.
        /// </summary>
        private static string Sha1Sum(string path)
        {
            if (Directory.Exists(path))
            {
                return null;
            }

            SHA1 hasher = new SHA1CryptoServiceProvider();

            // Even if we throw an exception, the using block here makes sure
            // we close our file.
            using (var fh = File.OpenRead(path))
            {
                string sha1 = BitConverter.ToString(hasher.ComputeHash(fh));
                fh.Close();
                return sha1;
            }
        }
    }

    /// <summary>
    /// A simple class that represents an installed module. Includes the time of installation,
    /// the module itself, and a list of files installed with it.
    /// 
    /// Primarily used by the Registry class.
    /// </summary>

    [JsonObject(MemberSerialization.OptIn)]
    public class InstalledModule
    {
        #region Fields and Properties

        // we never read from it and in the future we will read from it.
        #pragma warning disable 0414
        // ReSharper disable once NotAccessedField.Local
        [JsonProperty] private DateTime install_time;
        #pragma warning restore 0414

        [JsonProperty] private CfanJson source_module_json;

        private CfanModule source_module;

        // TODO: Our InstalledModuleFile already knows its path, so this could just
        // be a list. However we've left it as a dictionary for now to maintain
        // registry format compatibility.
        [JsonProperty] private Dictionary<string, InstalledModuleFile> installed_files;

        public IEnumerable<string> Files => installed_files.Keys;

        public string identifier => source_module.identifier;

        public CfanModule Module => source_module;

        #endregion

        #region Constructors

        public InstalledModule(KSP ksp, CfanModule module, IEnumerable<string> relative_files)
        {
            install_time = DateTime.Now;
            source_module_json = module.cfanJson;
            source_module = module;
            installed_files = new Dictionary<string, InstalledModuleFile>();

            foreach (string file in relative_files)
            {
                if (Path.IsPathRooted(file))
                {
                    throw new PathErrorKraken(file, "InstalledModule *must* have relative paths");
                }

                // IMF needs a KSP object so it can compute the SHA1.
                installed_files[file] = new InstalledModuleFile(file, ksp);
            }
        }

        [JsonConstructor]
        private InstalledModule()
        {
        }

        #endregion

        #region Serialisation Fixes


        [OnDeserialized]
        public void removeNulls(StreamingContext streamingContext)
        {
            source_module = new CfanModule(source_module_json);
        }

        /// <summary>
        /// Ensures all files for this module have relative paths.
        /// Called when upgrading registry versions. Should be a no-op
        /// if called on newer registries.</summary>
        public void Renormalise(KSP ksp)
        {
            var normalised_installed_files = new Dictionary<string, InstalledModuleFile>();

            foreach (KeyValuePair<string, InstalledModuleFile> tuple in installed_files)
            {
                string path = KSPPathUtils.NormalizePath(tuple.Key);

                if (Path.IsPathRooted(path))
                {
                    path = ksp.ToRelativeGameDataDir(path);
                }

                normalised_installed_files[path] = tuple.Value;
            }

            installed_files = normalised_installed_files;
        }

        #endregion
    }
}