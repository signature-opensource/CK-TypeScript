using System;

namespace CK.Core;

/// <summary>
/// Base class for folder resource handlers.
/// </summary>
public abstract class ResourceSpaceFolderHandler : IResourceSpaceHandler
{
    readonly IResourceSpaceItemInstaller? _installer;
    readonly string _rootFolderName;

    /// <summary>
    /// Initializes a new handler that will manage resources in the provided root folder.
    /// </summary>
    /// <param name="rootFolderName">Must not be empty, whitespace and there must be no '/' or '\' in it.</param>
    protected ResourceSpaceFolderHandler( IResourceSpaceItemInstaller? installer, string rootFolderName )
    {
        Throw.CheckArgument( !string.IsNullOrWhiteSpace( rootFolderName ) && !rootFolderName.AsSpan().ContainsAny( "\\/" ) );
        _installer = installer;
        _rootFolderName = rootFolderName;
    }

    /// <summary>
    /// Gets the root folder name.
    /// Never empty nor whitespace and doesn't contain '/' or '\'.
    /// </summary>
    public string RootFolderName => _rootFolderName;

    /// <summary>
    /// Gets the configured installer that <see cref="Install(IActivityMonitor)"/> will use.
    /// </summary>
    public IResourceSpaceItemInstaller? Installer => _installer;

    /// <summary>
    /// Must initialize this handler.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="spaceData">The space data to consider.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    internal protected abstract bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData );

    /// <summary>
    /// Called by <see cref="ResourceSpace.Install(IActivityMonitor)"/> (even if <see cref="Installer"/> is null).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>True on success, false otherwise.</returns>
    internal protected abstract bool Install( IActivityMonitor monitor );

    public sealed override string ToString() => $"{GetType().Name} - Folder '{_rootFolderName}/'";

}
