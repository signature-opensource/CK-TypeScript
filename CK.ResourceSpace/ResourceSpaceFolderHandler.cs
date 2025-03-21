using System;

namespace CK.Core;

/// <summary>
/// Base class for folder resource handlers.
/// </summary>
public abstract class ResourceSpaceFolderHandler
{
    readonly string _rootFolderName;

    /// <summary>
    /// Initializes a new handler that will manage resources in the provided root folder.
    /// </summary>
    /// <param name="rootFolderName">Must not be empty, whitespace and there must be no '/' or '\' in it.</param>
    protected ResourceSpaceFolderHandler( string rootFolderName )
    {
        Throw.CheckArgument( !string.IsNullOrWhiteSpace( rootFolderName ) && !rootFolderName.AsSpan().ContainsAny( "\\/" ) );
        _rootFolderName = rootFolderName;
    }

    /// <summary>
    /// Gets the root folder name.
    /// Never empty, whitespace and doesn't contain '/' or '\'.
    /// </summary>
    public string RootFolderName => _rootFolderName;

    /// <summary>
    /// Must initialize this handler.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="spaceData">The space data to consider.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    internal protected abstract bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData );

    public sealed override string ToString() => $"{GetType().Name} - Folder '{_rootFolderName}/'";
}
