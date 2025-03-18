using CK.Core;
using CK.EmbeddedResources;
using System.Collections.Generic;

namespace CK.TypeScript.LiveEngine;

/// <summary>
/// Assets state is managed inside the LiveState because the resources may need
/// to be accessed on change. Even if this uses the same idea as the TSLocales that
/// compacts one or more regular package assets in a (partial, except for the first ones)
/// <see cref="FinalResourceAssetSet"/>, a real <see cref="AssemblyResourceContainer"/> is required
/// instead of a fake <see cref="EmptyResourceContainer"/> to extract the resource content if needed.
/// </summary>
sealed class AssetsBuilder
{
    FinalResourceAssetSet? _currentRegularAssets;
    int _regularPackageCount;
    // A LocalPackageRef or a ResourceAssetSet of combined
    // resources of one or more regular packages.
    List<object> _packageAssets;
    Dictionary<NormalizedPath, FinalAsset>? _final;

    public AssetsBuilder()
    {
        _packageAssets = new List<object>();
    }

    public void AddRegularPackage( IActivityMonitor monitor, ResourceAssetDefinitionSet newSet )
    {
        Throw.DebugAssert( newSet != null );
        bool isPartial = _regularPackageCount > 0;
        _currentRegularAssets ??= new FinalResourceAssetSet( isPartial );
        _regularPackageCount++;
        _currentRegularAssets.Add( monitor, newSet );
    }

    public void AddLocalPackage( IActivityMonitor monitor, LocalPackageRef localPackage )
    {
        if( _currentRegularAssets != null )
        {
            _packageAssets.Add( _currentRegularAssets.Final );
            _currentRegularAssets = null;
        }
        _packageAssets.Add( localPackage );
    }

    internal void SetFinalAssets( ResourceAssetDefinitionSet final )
    {
        _final = new Dictionary<NormalizedPath, FinalAsset>();
        foreach( var (path,asset) in final.Assets )
        {
            _final[path] = FinalAsset.ToFinal( asset.Origin );
        }
    }

    public bool WriteState( CKBinaryWriter w,
                            CKBinaryWriter.ObjectPool<IResourceContainer> containerPool )
    {
        Throw.CheckState( "SetFinalAssets must have been called.", _final != null );
        return AssetsSerializer.WriteAssetsState( w, containerPool, _packageAssets, _final );
    }
}
