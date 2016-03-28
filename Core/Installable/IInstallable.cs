using System.IO;

namespace CKAN.Installable
{
    public interface IInstallable
    {
        bool makeDirs { get; }
        bool IsDirectory { get; }
        string Name { get; }
        string Destination { get; }
        Stream stream { get; }
    }
}