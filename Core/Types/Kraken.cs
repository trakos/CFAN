using System;
using System.Collections.Generic;
using CKAN.Factorio;

namespace CKAN
{
    /// <summary>
    /// Our application exceptions are called Krakens.
    /// </summary>
    public class Kraken : Exception
    {
        public Kraken(string reason = null, Exception inner_exception = null) : base(reason, inner_exception)
        {
        }
    }

    public class FileNotFoundKraken : Kraken
    {
        public string file;

        public FileNotFoundKraken(string file, string reason = null, Exception inner_exception = null) 
            :base(reason, inner_exception)
        {
            this.file = file;
        }
    }

    public class DirectoryNotFoundKraken : Kraken
    {
        public string directory;

        public DirectoryNotFoundKraken(string directory, string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
            this.directory = directory;
        }
    }

    /// <summary>
    /// A bad install location was provided.
    /// Valid locations are GameData, GameRoot, Ships, etc.
    /// </summary>
    public class BadInstallLocationKraken : Kraken
    {
        // Okay C#, you really need a keyword in your class declaration that says we call our
        // parent constructors by default. This sort of thing is unacceptable in a modern
        // programming langauge.

        public BadInstallLocationKraken(string reason = null, Exception inner_exception = null) : base(reason, inner_exception)
        {
        }
    }

    public class ModuleNotFoundKraken : Kraken
    {
        public string module;
        public string version;

        // TODO: Is there a way to set the stringify version of this?
        public ModuleNotFoundKraken(string module, string version = null, string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
            this.module = module;
            this.version = version;
        }
    }

    public class NotFactorioDirectoryKraken : Kraken
    {
        public string path;

        public NotFactorioDirectoryKraken(string path, string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
            this.path = path;
        }
    }

    public class NotFactorioDataDirectoryKraken : Kraken
    {
        public string path;

        public NotFactorioDataDirectoryKraken(string path, string reason = null, Exception inner_exception = null)
            : base(reason, inner_exception)
        {
            this.path = path;
        }
    }

    public class TransactionalKraken : Kraken
    {
        public TransactionalKraken(string reason = null, Exception inner_exception = null)
            :base(reason,inner_exception)
        {
        }
    }

    /// <summary>
    /// We had bad metadata that resulted in an invalid operation occuring.
    /// For example: a file install stanza that produces no files.
    /// </summary>
    public class BadMetadataKraken : Kraken
    {
        public CfanModule module;

        public BadMetadataKraken(CfanModule module, string reason = null, Exception inner_exception = null)
            :base(reason,inner_exception)
        {
            this.module = module;
        }
    }

    /// <summary>
    /// Thrown if we try to load an incompatible CKAN registry.
    /// </summary>
    public class RegistryVersionNotSupportedKraken : Kraken
    {
        public int requested_version;

        public RegistryVersionNotSupportedKraken(int v, string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
            requested_version = v;
        }
    }

    public class TooManyModsProvideKraken : Kraken
    {
        public List<CfanModule> modules;
        public string requested;

        public TooManyModsProvideKraken(string requested, List<CfanModule> modules, Exception inner_exception = null)
            :base(FormatMessage(requested, modules), inner_exception)
        {
            this.modules = modules;
            this.requested = requested;
        }

        internal static string FormatMessage(string requested, List<CfanModule> modules)
        {
            string oops = string.Format("Too many mods provide {0}:\n", requested);
            return oops + String.Join("\n* ", modules);
        }
    }

    /// <summary>
    /// Thrown if we find ourselves in an inconsistent state, such as when we have multiple modules
    /// installed which conflict with each other.
    /// </summary>
    public class InconsistentKraken : Kraken
    {
        public ICollection<string> inconsistencies;

        public string InconsistenciesPretty
        {
            get
            {
                const string message = "The following inconsistencies were found:\n";
                return message + String.Join("\n * ", inconsistencies);
            }
        }

        public InconsistentKraken(ICollection<string> inconsistencies, Exception inner_exception = null)
            :base(null, inner_exception)
        {
            this.inconsistencies = inconsistencies;
        }

        public InconsistentKraken(string inconsistency, Exception inner_exception = null)
            :base(null, inner_exception)
        {
            inconsistencies = new List<string> { inconsistency };
        }

        public override string ToString()
        {
            return InconsistenciesPretty + StackTrace;
        }
    }

    /// <summary>
    /// The terrible state when a file exists when we expect it not to be there.
    /// For example, when we install a mod, and it tries to overwrite a file from another mod.
    /// </summary>
    public class FileExistsKraken : Kraken
    {
        public string filename;

        // These aren't set at construction time, but exist so that we can decorate the
        // kraken as appropriate.
        public CfanModule installing_module;
        public string owning_module;

        public FileExistsKraken(string filename, string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
            this.filename = filename;
        }
    }

    /// <summary>
    /// The terrible state when errors occurred during downloading.
    /// Requires an IEnumerable list of exceptions on construction.
    /// Has a specialised ToString() that shows everything that went wrong.
    /// </summary>
    public class DownloadErrorsKraken : Kraken
    {
        public List<Exception> exceptions;

        public DownloadErrorsKraken(IEnumerable<Exception> errors, string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
            exceptions = new List<Exception> (errors);
        }

        public override string ToString()
        {
            return "Uh oh, the following things went wrong when downloading...\n\n" + String.Join("\n", exceptions);
        }
    }

    /// <summary>
    /// The terrible kraken summoned forth to indicate a user cancelled whatever
    /// we were doing.
    /// </summary>
    public class CancelledActionKraken : Kraken
    {
        public CancelledActionKraken(string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
        }
    }

    /// <summary>
    /// The terrible kraken that awakens from the deep when we don't support something,
    /// like a metadata spec from the future.
    /// </summary>
    public class UnsupportedKraken : Kraken
    {
        public UnsupportedKraken(string reason, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
        }
    }

    /// <summary>
    /// The mighty kraken that emerges from the depth when we have a problem with a path,
    /// such as when it cannot be converted from absolute to relative, or vice-versa.
    /// </summary>
    public class PathErrorKraken : Kraken
    {
        public string path;

        public PathErrorKraken(string path, string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
            this.path = path;
        }
    }

    /// <summary>
    /// Tremble, mortal, for ye has summoned the kraken of mods which are not installed.
    /// Thou hast tried to remove or perform actions upon a mod that is not there!
    /// This kraken provides a custom Message
    /// </summary>
    public class ModNotInstalledKraken : Kraken
    {
        public string mod;

        public override string Message
        {
            get { return string.Format("Module {0} is not installed!", mod); }
        }

        // TODO: Since we override message, should we really allow users to pass in a reason
        // here? Is there a way we can check if that was set, and then access it directly from
        // our base class?

        public ModNotInstalledKraken(string mod, string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
            this.mod = mod;
        }
    }

    /// <summary>
    /// A bad command; useful for things like command-line handling, or REST servers.
    /// </summary>
    public class BadCommandKraken : Kraken
    {
        public BadCommandKraken(string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
        }
    }

    public class MissingCertificateKraken : Kraken
    {
        public MissingCertificateKraken(string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
        }

        public override string ToString()
        {
            return
                "\nOh no! Our download failed with a certificate error!\n\n" +
                "If you're on Linux, try running:\n" +
                "\tmozroots --import --ask-remove\n" +
                "on the command-line to update your certificate store, and try again.\n\n"
            ;
        }
    }

    public class InvalidEntryInModsDirectoryKraken : Kraken
    {
        public InvalidEntryInModsDirectoryKraken(string reason = null, Exception inner_exception = null)
            :base(reason, inner_exception)
        {
        }
    }

    public class NetfanDownloadKraken : Kraken
    {
        public NetfanDownloadKraken(string reason = null, Exception inner_exception = null)
            : base(reason, inner_exception)
        {
        }
    }

    public class CachedPreviousDownloadErrorKraken : NetfanDownloadKraken
    {
        public CachedPreviousDownloadErrorKraken(string reason = null, Exception inner_exception = null)
            : base(reason, inner_exception)
        {
        }
    }

    public class HtmlInsteadOfModDownloadedKraken : NetfanDownloadKraken
    {
        public HtmlInsteadOfModDownloadedKraken(string reason = null, Exception inner_exception = null)
            : base(reason, inner_exception)
        {
        }
    }

    public class BadVersionKraken : Kraken
    {
        private readonly string versionString;

        public BadVersionKraken(string versionString,  Exception innerException = null)
            : base(createReasonString(versionString), innerException)
        {
            this.versionString = versionString;
        }

        protected static string createReasonString(string versionString)
        {
            return $"[BadVersionKraken] {versionString} is not a valid version string";
        }

        public override string ToString()
        {
            return createReasonString(versionString);
        }
    }

    public class ModuleAndVersionStringInvalidKraken : Kraken
    {
        public readonly string givenString;

        public ModuleAndVersionStringInvalidKraken(string givenString, Exception innerException = null)
            : base(createReasonString(givenString), innerException)
        {
            this.givenString = givenString;
        }

        protected static string createReasonString(string versionString)
        {
            return $"[ModuleAndVersionStringInvalidKraken] {versionString} is not a valid module=version string";
        }

        public override string ToString()
        {
            return createReasonString(givenString);
        }
    }
}
