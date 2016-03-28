using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CKAN.Installable
{
    public class InstallableFile : IInstallable
    {
        protected string absolutePathToFile;
        protected string destinationRelativeToModsRoot;

        public InstallableFile(string absolutePathToFile, string destinationRelativeToModsRoot)
        {
            this.absolutePathToFile = absolutePathToFile;
            this.destinationRelativeToModsRoot = destinationRelativeToModsRoot;
        }


        public bool makeDirs => true;
        public bool IsDirectory => false;
        public string Name => Path.GetFileName(destinationRelativeToModsRoot);
        public string Destination => KSPPathUtils.NormalizePath(destinationRelativeToModsRoot);
        public Stream stream => File.OpenRead(absolutePathToFile);
    }
}
