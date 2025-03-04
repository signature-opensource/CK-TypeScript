using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Base class for <see cref="ResourceSpace.Handlers"/>. Handles resources either by the root
/// folder name (like "locales/*") or by file extension (like "*.ts").
/// </summary>
public abstract class ResourceSpaceHandler
{
    readonly string? _rootFolderName;
    readonly ImmutableArray<string> _fileExtensions;

    /// <summary>
    /// Initializes a new handler that will manage resources in the provided root folder.
    /// </summary>
    /// <param name="rootFolderName">See <see cref="RootFolderName"/>.</param>
    protected ResourceSpaceHandler( string rootFolderName )
    {
        _rootFolderName = rootFolderName;
        _fileExtensions = ImmutableArray<string>.Empty;
    }

    /// <summary>
    /// Initializes a new handler that will manage resources with the provided file extensions.
    /// </summary>
    /// <param name="fileExtensions">See <see cref="FileExtensions"/>.</param>
    protected ResourceSpaceHandler( params ImmutableArray<string> fileExtensions )
    {
        Throw.CheckArgument( fileExtensions.Length > 0 && fileExtensions.All( e => e.Length >= 2 && e[0] == '.' ) );
        _fileExtensions = fileExtensions;
    }

    /// <summary>
    /// Gets the root folder name. There should be no '/' or '\' in it.
    /// When null, this handler manages the selects its resources by
    /// their <see cref="FileExtensions"/>.
    /// </summary>
    public string? RootFolderName => _rootFolderName;

    /// <summary>
    /// Gets the file extensions that will be handled by this handler.
    /// When empty, the <see cref="RootFolderName"/> is not null.
    /// </summary>
    public ImmutableArray<string> FileExtensions => _fileExtensions;

    public override string ToString()
    {
        return _rootFolderName != null
                ? $"{GetType().Name} - Folder '{_rootFolderName}/'"
                : $"{GetType().Name} - Files '*{_fileExtensions.Concatenate( "', *'" )}'";
    }

}
