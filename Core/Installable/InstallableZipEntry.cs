using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace CKAN.Installable
{
    public class InstallableZipEntry : IInstallable
    {
        protected ZipEntry zipEntry;
        protected ZipFile zipFile;

        public InstallableZipEntry(ZipEntry zipEntry, ZipFile zipFile)
        {
            this.zipEntry = zipEntry;
            this.zipFile = zipFile;
        }


        public bool makeDirs => true;
        public bool IsDirectory => zipEntry.IsDirectory;
        public string Name => zipEntry.Name;
        public string Destination => KSPPathUtils.NormalizePath(zipEntry.Name);
        public Stream stream => zipFile.GetInputStream(zipEntry);
    }
}