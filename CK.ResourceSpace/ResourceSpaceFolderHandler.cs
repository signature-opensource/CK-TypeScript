using System;

namespace CK.Core;

/// <summary>
/// Base class for folder resource handlers.
/// </summary>
public abstract class ResourceSpaceFolderHandler
{
    readonly ResourceSpaceData _spaceData;
    readonly string _rootFolderName;

    /// <summary>
    /// Initializes a new handler that will manage resources in the provided root folder.
    /// </summary>
    /// <param name="rootFolderName">Must not be empty, whitespace and there must be no '/' or '\' in it.</param>
    protected ResourceSpaceFolderHandler( ResourceSpaceData spaceData, string rootFolderName )
    {
        Throw.CheckArgument( !string.IsNullOrWhiteSpace( rootFolderName ) && !rootFolderName.AsSpan().ContainsAny( "\\/" ) );
        _spaceData = spaceData;
        _rootFolderName = rootFolderName;
    }

    /// <summary>
    /// Gets the root folder name.
    /// Never empty, whitespace and doesn't contain '/' or '\'.
    /// </summary>
    public string RootFolderName => _rootFolderName;

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
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    internal protected abstract bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData );

    public sealed override string ToString() => $"{GetType().Name} - Folder '{_rootFolderName}/'";
}
