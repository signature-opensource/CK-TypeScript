using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

public sealed class ResourceSpace
{
    readonly Dictionary<object, ResPackage> _packageIndex;
    readonly ImmutableArray<ResourceSpaceFolderHandler> _folderHandlers;
    readonly ImmutableArray<ResourceSpaceFileHandler> _fileHandlers;
    internal ImmutableArray<ResPackage> _packages;
    readonly ResourceSpaceFileHandler.FolderExclusion _folderExclusion;
    
    public ResourceSpace( Dictionary<object, ResPackage> packageIndex,
                          ImmutableArray<ResourceSpaceFolderHandler> folderHandlers,
                          ImmutableArray<ResourceSpaceFileHandler> fileHandlers )
    {
        _packageIndex = packageIndex;
        _folderHandlers = folderHandlers;
        _fileHandlers = fileHandlers;
        _folderExclusion = new ResourceSpaceFileHandler.FolderExclusion( folderHandlers );
    }

    internal bool Initialize( IActivityMonitor monitor )
    {
        bool success = true;
        foreach( var h in _folderHandlers )
        {
            success &= h.Initialize( monitor, _packages );
        }
        foreach( var h in _fileHandlers )
        {
            success &= h.Initialize( monitor, _packages, _folderExclusion );
        }
        return success;
    }

}
