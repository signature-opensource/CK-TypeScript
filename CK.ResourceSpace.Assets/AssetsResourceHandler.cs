
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

sealed class AggregateSetCache
{
    public AggregateSetCache()
    {
    }

    public IEnumerable<IResPackageResources> 
}

public class AssetsResourceHandler : ResourceSpaceFolderHandler
{
    readonly FinalResourceAssetSet[] _finals;

    public AssetsResourceHandler( ResourceSpaceData spaceData, string rootFolderName )
        : base( spaceData, rootFolderName )
    {
        _finals = new FinalResourceAssetSet[spaceData.AllPackageResources.Length];
    }

    protected override bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData )
    {
        bool success = true;
        foreach( var r in spaceData.AllPackageResources )
        {
            var baseSet = FinalResourceAssetSet.Empty.Aggregate( r.Reachables.Select( r => _finals[r.Index] ) );
            if( r.Resources.LoadAssets( monitor, r.Package.DefaultTargetPath, out var definitions, RootFolderName ) )
            {
                if( definitions == null )
                {
                    _finals[r.Index] = baseSet;
                }
                else
                {
                    var f = definitions.Combine( monitor, baseSet );
                    if( f == null )
                    {
                        success = false;
                        // Fallback to the baseSet: no null in the array even on error.
                        f = baseSet;
                    }
                    _finals[r.Index] = f;
                }
            }
            else
            {
                success = false;
            }
        }

    }
}
