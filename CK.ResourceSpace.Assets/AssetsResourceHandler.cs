
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

sealed class AssetCache : ReachablePackageDataCache<FinalResourceAssetSet>
{
    readonly AssetsResourceHandler _handler;

    public AssetCache( AssetsResourceHandler handler, ReachablePackageSetCache cache )
        : base( cache )
    {
        _handler = handler;
    }

    protected override FinalResourceAssetSet Aggregate( FinalResourceAssetSet data1, FinalResourceAssetSet data2 )
    {
        return data1.Aggregate( data2 );
    }

    protected override FinalResourceAssetSet? Create( IActivityMonitor monitor, ResPackage package )
    {
        FinalResourceAssetSet? assets = LoadInitialSet( monitor, package );
        if( assets == null ) return null;
        if( package.AfterReachablePackages.Count > 0 )
        {
            assets = Get( monitor, package.AfterReachablePackages )?.Aggregate( assets );
            if( assets == null ) return null;
        }
        if( package.AfterResources.Resources.LoadAssets( monitor,
                                                         package.DefaultTargetPath,
                                                         out var afterDefinitions,
                                                         _handler.RootFolderName ) )
        {
            if( afterDefinitions != null )
            {
                assets = afterDefinitions.Combine( monitor, assets );
            }
        }
        return assets;
    }

    private FinalResourceAssetSet? LoadInitialSet( IActivityMonitor monitor, ResPackage package )
    {
        FinalResourceAssetSet? initial = null;
        if( package.BeforeResources.Resources.LoadAssets( monitor,
                                                           package.DefaultTargetPath,
                                                           out var definitions,
                                                           _handler.RootFolderName ) )
        {
            initial = definitions != null
                            ? definitions.ToInitialFinalSet( monitor )
                            : FinalResourceAssetSet.Empty;
        }

        return initial;
    }
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
