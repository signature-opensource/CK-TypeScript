using CK.Core;
using CK.EmbeddedResources;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.LiveEngine;

static class AssetsSerializer
{
    static void WriteResourceAssetSet( CKBinaryWriter w,
                                       ResourceAssetDefinitionSet c,
                                       CKBinaryWriter.ObjectPool<IResourceContainer> containerPool )
    {
        w.WriteNonNegativeSmallInt32( c.Assets.Count );
        foreach( var (path, asset) in c.Assets )
        {
            w.Write( path.Path );
            StateSerializer.WriteResourceLocator( w, containerPool, asset.Origin );
            w.Write( (byte)asset.Override );
        }
    }

    static ResourceAssetDefinitionSet ReadResourceAssetSet( CKBinaryReader r,
                                                  CKBinaryReader.ObjectPool<IResourceContainer> containerPool )
    {
        int count = r.ReadNonNegativeSmallInt32();
        var assets = new Dictionary<NormalizedPath, ResourceAssetDefinition>( count );
        while( --count >= 0 )
        {
            NormalizedPath  key = r.ReadString();
            var origin = StateSerializer.ReadResourceLocator( r, containerPool );
            assets.Add( key, new ResourceAssetDefinition( origin, (ResourceOverrideKind)r.ReadByte() ) );
        }
        return new ResourceAssetDefinitionSet( assets );
    }

    public static bool WriteAssetsState( CKBinaryWriter w,
                                         CKBinaryWriter.ObjectPool<IResourceContainer> containerPool,
                                         List<object> packageAssets,
                                         Dictionary<NormalizedPath, FinalAsset> finalAssets )
    {
        w.WriteNonNegativeSmallInt32( packageAssets.Count );
        foreach( var locale in packageAssets )
        {
            if( locale is ResourceAssetDefinitionSet c )
            {
                w.WriteSmallInt32( -1 );
                WriteResourceAssetSet( w, c, containerPool );
            }
            else
            {
                w.WriteSmallInt32( Unsafe.As<LocalPackageRef>( locale ).IdxLocal );
            }
        }
        w.WriteNonNegativeSmallInt32( finalAssets.Count );
        foreach( var (path, asset) in finalAssets )
        {
            w.Write( path.Path );
            StateSerializer.WriteResourceLocator( w, containerPool, asset.Origin );
            w.WriteNullableString( asset.LocalPath );
            w.Write( asset.LastWriteTime );
        }
        return true;
    }

    internal static (IAssetPackage[]?, Dictionary<NormalizedPath, FinalAsset> Final)
                        ReadAssetsState( CKBinaryReader r,
                                         ImmutableArray<LocalPackage> localPackages,
                                         CKBinaryReader.ObjectPool<IResourceContainer> containerPool )
    {
        int count = r.ReadNonNegativeSmallInt32();
        var b = new IAssetPackage[count];
        for( int i = 0; i < count; i++ )
        {
            var idx = r.ReadSmallInt32();
            b[i] = idx < 0
                    ? new LiveAssets.Regular( ReadResourceAssetSet( r, containerPool ) )
                    : new LiveAssets.Local( localPackages[idx] );
        }
        count = r.ReadNonNegativeSmallInt32();
        var assets = new Dictionary<NormalizedPath, FinalAsset>( count );
        for( int i = 0; i < count; i++ )
        {
            NormalizedPath key = r.ReadString();
            var origin = StateSerializer.ReadResourceLocator( r, containerPool );
            assets.Add( key, new FinalAsset( origin, r.ReadNullableString(), r.ReadDateTime() ) );
        }
        return (b, assets);
    }

}
