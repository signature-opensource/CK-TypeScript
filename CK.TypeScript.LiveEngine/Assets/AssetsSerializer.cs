using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CK.TypeScript.LiveEngine;

static class AssetsSerializer
{
    static void WriteResourceAssetSet( CKBinaryWriter w,
                                       ResourceAssetSet c,
                                       CKBinaryWriter.ObjectPool<IResourceContainer> containerPool,
                                       CKBinaryWriter.ObjectPool<AssemblyResourceContainer> assemblyPool )
    {
        w.WriteNonNegativeSmallInt32( c.Assets.Count );
        foreach( var (path, asset) in c.Assets )
        {
            w.Write( path.Path );
            StateSerializer.WriteResourceLocator( w, containerPool, asset.Origin, assemblyPool );
            w.Write( (byte)asset.Override );
        }
    }

    static ResourceAssetSet ReadResourceAssetSet( CKBinaryReader r,
                                                  CKBinaryReader.ObjectPool<IResourceContainer> containerPool,
                                                  CKBinaryReader.ObjectPool<AssemblyResourceContainer> assemblyPool )
    {
        int count = r.ReadNonNegativeSmallInt32();
        var assets = new Dictionary<NormalizedPath, ResourceAsset>( count );
        while( --count >= 0 )
        {
            NormalizedPath  key = r.ReadString();
            var origin = StateSerializer.ReadResourceLocator( r, containerPool, assemblyPool );
            assets.Add( key, new ResourceAsset( origin, (ResourceOverrideKind)r.ReadByte() ) );
        }
        return new ResourceAssetSet( assets );
    }

    public static bool WriteAssetsState( CKBinaryWriter w,
                                         CKBinaryWriter.ObjectPool<IResourceContainer> containerPool,
                                         CKBinaryWriter.ObjectPool<AssemblyResourceContainer> assemblyPool,
                                         List<object> packageAssets,
                                         Dictionary<NormalizedPath, FinalAsset> finalAssets )
    {
        w.WriteNonNegativeSmallInt32( packageAssets.Count );
        foreach( var locale in packageAssets )
        {
            if( locale is ResourceAssetSet c )
            {
                w.WriteSmallInt32( -1 );
                WriteResourceAssetSet( w, c, containerPool, assemblyPool );
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
            StateSerializer.WriteResourceLocator( w, containerPool, asset.Origin, assemblyPool );
            w.Write( asset.LastWriteTime );
        }
        return true;
    }

    internal static (IAssetPackage[]?, Dictionary<NormalizedPath, FinalAsset> Final)
                        ReadAssetsState( CKBinaryReader r,
                                         ImmutableArray<LocalPackage> localPackages,
                                         CKBinaryReader.ObjectPool<IResourceContainer> containerPool,
                                                      CKBinaryReader.ObjectPool<AssemblyResourceContainer> assemblyPool )
    {
        int count = r.ReadNonNegativeSmallInt32();
        var b = new IAssetPackage[count];
        for( int i = 0; i < count; i++ )
        {
            var idx = r.ReadSmallInt32();
            b[i] = idx < 0
                    ? new LiveAssets.Regular( ReadResourceAssetSet( r, containerPool, assemblyPool ) )
                    : new LiveAssets.Local( localPackages[idx] );
        }
        count = r.ReadNonNegativeSmallInt32();
        var assets = new Dictionary<NormalizedPath, FinalAsset>( count );
        for( int i = 0; i < count; i++ )
        {
            NormalizedPath key = r.ReadString();
            var origin = StateSerializer.ReadResourceLocator( r, containerPool, assemblyPool );
            assets.Add( key, new FinalAsset( origin, r.ReadDateTime() ) );
        }
        return (b, assets);
    }

}
