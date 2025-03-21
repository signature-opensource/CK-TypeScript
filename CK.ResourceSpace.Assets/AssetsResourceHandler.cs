
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

public class AssetsResourceHandler : ResourceSpaceFolderHandler
{
    readonly AssetCache _cache;

    public AssetsResourceHandler( IResPackageDataCache packageDataCache, string rootFolderName )
        : base( rootFolderName )
    {
        _cache = new AssetCache( this, packageDataCache );
    }

    protected override bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData )
    {
        bool success = true;
        foreach( var p in spaceData.Packages )
        {
            success &= _cache.Obtain( monitor, p ) != null;
        }
        return success;
    }
}
