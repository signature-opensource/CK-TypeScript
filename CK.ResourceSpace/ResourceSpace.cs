using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Final production initiated by a <see cref="ResourceSpaceCollectorBuilder"/>.
/// Once available, all registered <see cref="FolderHandlers"/> and <see cref="FileHandlers"/>
/// have been successfully initialized.
/// </summary>
public sealed class ResourceSpace
{
    readonly ResourceSpaceData _data;
    readonly ImmutableArray<ResourceSpaceFolderHandler> _folderHandlers;
    readonly ImmutableArray<ResourceSpaceFileHandler> _fileHandlers;
    readonly ResourceSpaceFileHandler.FolderExclusion _folderExclusion;
    
    internal ResourceSpace( ResourceSpaceData data,
                            ImmutableArray<ResourceSpaceFolderHandler> folderHandlers,
                            ImmutableArray<ResourceSpaceFileHandler> fileHandlers )
    {
        _data = data;
        _folderHandlers = folderHandlers;
        _fileHandlers = fileHandlers;
        _folderExclusion = new ResourceSpaceFileHandler.FolderExclusion( folderHandlers );
    }

    /// <summary>
    /// Gets the resources data.
    /// </summary>
    public ResourceSpaceData ResourceSpaceData => _data;

    /// <summary>
    /// Gets the successfully initialized <see cref="ResourceSpaceFolderHandler"/>.
    /// </summary>
    public ImmutableArray<ResourceSpaceFolderHandler> FolderHandlers => _folderHandlers;

    /// <summary>
    /// Gets the successfully initialized <see cref="ResourceSpaceFileHandler"/>.
    /// </summary>
    public ImmutableArray<ResourceSpaceFileHandler> FileHandlers => _fileHandlers;

    internal bool Initialize( IActivityMonitor monitor )
    {
        bool success = true;
        foreach( var h in _folderHandlers )
        {
            success &= h.Initialize( monitor, _data );
        }
        foreach( var h in _fileHandlers )
        {
            success &= h.Initialize( monitor, _data, _folderExclusion );
        }
        return success;
    }
}
