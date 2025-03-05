using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;


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
