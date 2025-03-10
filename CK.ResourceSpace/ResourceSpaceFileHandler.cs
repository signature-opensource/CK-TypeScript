using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Base class for file resource handlers.
/// </summary>
public abstract partial class ResourceSpaceFileHandler
{
    readonly ResourceSpaceData _spaceData;
    readonly ImmutableArray<string> _fileExtensions;

    /// <summary>
    /// Initializes a new handler that will manage resources with the provided file extensions.
    /// </summary>
    /// <param name="spaceData">Target space data.</param>
    /// <param name="fileExtensions">One or more file extensions that must start with '.' (like ".css").</param>
    protected ResourceSpaceFileHandler( ResourceSpaceData spaceData, params ImmutableArray<string> fileExtensions )
    {
        Throw.CheckArgument( fileExtensions.Length > 0 && fileExtensions.All( e => e.Length >= 2 && e[0] == '.' ) );
        _spaceData = spaceData;
        _fileExtensions = fileExtensions;
    }

    /// <summary>
    /// Gets the file extensions that will be handled by this handler.
    /// </summary>
    public ImmutableArray<string> FileExtensions => _fileExtensions;

    /// <summary>
    /// Gets <see cref="ResourceSpaceData"/>.
    /// </summary>
    protected ResourceSpaceData SpaceData => _spaceData;

    /// <summary>
    /// Must initialize this handler from the <see cref="ResourceSpaceData"/> that
    /// exposes the ordered set of packages and the <see cref="ResourceSpaceData.PackageIndex"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="spaceData">The space data to consider.</param>
    /// <param name="folderFilter">The filter for resources or folder to use.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    internal abstract bool Initialize( IActivityMonitor monitor,
                                       ResourceSpaceData spaceData,
                                       FolderExclusion folderFilter );

    public sealed override string ToString() => $"{GetType().Name} - Files '*{_fileExtensions.Concatenate( "', *'" )}'";

}
