
using CK.EmbeddedResources;
using System;
using System.Linq;

namespace CK.Core;

public partial class AssetsResourceHandler : ResourceSpaceFolderHandler
{
    readonly AssetCache _cache;
    FinalResourceAssetSet? _finalAssets;

    public AssetsResourceHandler( IResourceSpaceItemInstaller? installer,
                                  ISpaceDataCache packageDataCache,
                                  string rootFolderName )
        : base( installer, rootFolderName )
    {
        _cache = new AssetCache( packageDataCache, rootFolderName );
    }

    /// <summary>
    /// Gets the cache instance to which this data handler is bound.
    /// </summary>
    public ISpaceDataCache ResPackageDataCache => _cache.SpaceCache;

    /// <summary>
    /// Gets the final assets that have been successfully initialized.
    /// <see cref="FinalResourceAssetSet.IsAmbiguous"/> is necessarily false.
    /// </summary>
    public FinalResourceAssetSet? FinalAssets => _finalAssets;

    protected override bool Initialize( IActivityMonitor monitor, ResSpaceData spaceData )
    {
        FinalResourceAssetSet? r = _cache.Obtain( monitor, spaceData.AppPackage );
        if( r == null ) return false;
        if( r.IsAmbiguous )
        {
            var ambiguities = r.Assets.Where( kv => kv.Value.Ambiguities != null )
                                      .Select( kv => $"'{kv.Key}' is mapped by {kv.Value.Origin} but also to {kv.Value.Ambiguities!.Select( r => r.ToString() ).Concatenate()}." );
            monitor.Error( $"""
                Ambiguities detected in assets:
                {ambiguities.Concatenate( Environment.NewLine )}
                """ );
            return false;
        }
        _finalAssets = r;
        return true;
    }

    /// <summary>
    /// Saves the initialized <see cref="FinalAssets"/> into this <see cref="ResourceSpaceFolderHandler.Installer"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>True on succes, false one error (errors have been logged).</returns>
    protected override bool Install( IActivityMonitor monitor )
    {
        if( Installer is null )
        {
            monitor.Warn( $"No installer associated to '{ToString()}'. Skipped." );
            return true;
        }
        Throw.CheckState( FinalAssets != null );
        try
        {
            foreach( var a in FinalAssets.Assets )
            {
                Installer.Write( a.Key, a.Value.Origin );
            }
            return true;
        }
        catch( Exception ex )
        {
            monitor.Error( "While generating Assets.", ex );
            return false;
        }
    }
}
