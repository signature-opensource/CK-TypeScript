
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

public class AssetsResourceHandler : ResourceSpaceFolderHandler
{
    readonly AssetCache _cache;
    FinalResourceAssetSet? _finalAssets;

    public AssetsResourceHandler( IResPackageDataCache packageDataCache, string rootFolderName )
        : base( rootFolderName )
    {
        _cache = new AssetCache( this, packageDataCache );
    }

    /// <summary>
    /// Gets the cache instance to which this data handler is bound.
    /// </summary>
    public IResPackageDataCache ResPackageDataCache => _cache.ResPackageDataCache;

    /// <summary>
    /// Gets the final assets that have been successfully initialized.
    /// <see cref="FinalResourceAssetSet.IsAmbiguous"/> is necessarily false.
    /// </summary>
    public FinalResourceAssetSet? FinalAssets => _finalAssets; 

    protected override bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData )
    {
        FinalResourceAssetSet? r = _cache.Obtain( monitor, spaceData.AppPackage );
        if( r == null ) return false;
        if( r.IsAmbiguous )
        {
            var ambiguities = r.Assets.Where( kv => kv.Value.Ambiguities != null )
                                      .Select( kv => $"'{kv.Key}' is mapped by {kv.Value.Origin} but also to {kv.Value.Ambiguities!.Select( r => r.ToString()).Concatenate()}." );
            monitor.Error( $"""
                Ambiguities detected in assets:
                {ambiguities.Concatenate(Environment.NewLine)}
                """ );
            return false;
        }
        _finalAssets = r;
        return true;
    }

    /// <summary>
    /// Saves the initialized <see cref="FinalAssets"/> into the <paramref name="target"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="target">The target.</param>
    public void Install( IActivityMonitor monitor, ResourceSpaceFileInstaller target )
    {
        Throw.CheckState( FinalAssets != null );
        foreach( var a in FinalAssets.Assets )
        {
            target.Write( a.Key, a.Value.Origin );
        }
    }

}
