using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Base class for file resource handlers.
/// </summary>
public abstract partial class ResourceSpaceFileHandler
{
    readonly ImmutableArray<string> _fileExtensions;

    /// <summary>
    /// Initializes a new handler that will manage resources with the provided file extensions.
    /// </summary>
    /// <param name="fileExtensions">One or more file extensions that must start with '.' (like ".css").</param>
    protected ResourceSpaceFileHandler( params ImmutableArray<string> fileExtensions )
    {
        Throw.CheckArgument( fileExtensions.Length > 0 && fileExtensions.All( e => e.Length >= 2 && e[0] == '.' ) );
        _fileExtensions = fileExtensions;
    }

    /// <summary>
    /// Gets the file extensions that will be handled by this handler.
    /// </summary>
    public ImmutableArray<string> FileExtensions => _fileExtensions;

    /// <summary>
    /// Must initialize this handler.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="spaceData">The space data to consider.</param>
    /// <param name="folderFilter">The filter for resources or folder to use.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    internal protected abstract bool Initialize( IActivityMonitor monitor,
                                                 ResourceSpaceData spaceData,
                                                 FolderExclusion folderFilter );

    /// <summary>
    /// Called by <see cref="ResourceSpace.Install(IActivityMonitor)"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="installer">The target installer.</param>
    /// <returns>True on success, false otherwise.</returns>
    internal protected abstract bool Install( IActivityMonitor monitor, IResourceSpaceFileInstaller installer );

    public sealed override string ToString() => $"{GetType().Name} - Files '*{_fileExtensions.Concatenate( "', *'" )}'";

}
