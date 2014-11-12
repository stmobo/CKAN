using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Runtime.Serialization;
using log4net;

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
            string absolute_path = ksp.ToAbsolute(path);
            this.sha1_sum = Sha1Sum(absolute_path);
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
    /// A simple clss that represents an installed module. Includes the time of installation,
    /// the module itself, and a list of files installed with it.
    /// 
    /// Primarily used by the Registry class.
    /// </summary>

    [JsonObject(MemberSerialization.OptIn)]
    public class InstalledModule
    {
        [JsonProperty] private DateTime install_time;
        [JsonProperty] private Module source_module;

        private static readonly ILog log = LogManager.GetLogger(typeof(InstalledModule));

        // TODO: Our InstalledModuleFile already knows its path, so this could just
        // be a list. However we've left it as a dictionary for now to maintain
        // registry format compatibility.
        [JsonProperty] private Dictionary<string, InstalledModuleFile> installed_files;

        public IEnumerable<string> Files 
        {
            get
            {
                return installed_files.Keys;
            }
        }

        public string identifier
        {
            get
            {
                return source_module.identifier;
            }
        }

        public Module Module
        {
            get
            {
                return source_module;
            }
        }

        public InstalledModule(KSP ksp, Module module, IEnumerable<string> relative_files)
        {
            this.install_time = DateTime.Now;
            this.source_module = module;
            this.installed_files = new Dictionary<string, InstalledModuleFile>();

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

        // If we're being deserialised from our JSON file, we don't need to
        // do any special construction.
        [JsonConstructor]
        private InstalledModule()
        {
        }
    }
}