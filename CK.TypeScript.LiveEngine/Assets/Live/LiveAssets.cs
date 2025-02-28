using CK.Core;
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CK.TypeScript.LiveEngine;

sealed partial class LiveAssets
{
    readonly LiveState _state;
    ImmutableArray<IAssetPackage> _packages;
    Dictionary<NormalizedPath, FinalAsset>? _final;
    bool _isDirty;

    public LiveAssets( LiveState state )
    {
        _state = state;
    }

    public bool IsDirty => _isDirty;

    internal bool Load( IActivityMonitor monitor,
                        CKBinaryReader r,
                        CKBinaryReader.ObjectPool<IResourceContainer> containerPool )
    {
        (var packages, _final) = AssetsSerializer.ReadAssetsState( r, _state.LocalPackages, containerPool );
        _packages = ImmutableCollectionsMarshal.AsImmutableArray( packages );
        return true;
    }

    internal void OnChange( IActivityMonitor monitor, LocalPackage? package, string subPath )
    {
        Throw.DebugAssert( _final != null );
        _isDirty = true;

        // We can be much more clever here by maintaining a
        // Dictionary<string,DateTime> or other data structure
        // that would enable to track changes on a per file basis.
        // However, the "brand new file" in a local package would need
        // to be handled explicitly. For the moment, any change triggers
        // a full recompute of the assets and a differential update of
        // the ck-gen/assets final folder.

        // if( subPath == "assets/assets.jsonc" )
        // {
        //     _isDirty = true;
        // }
        // else
        // {
        //     // Per-file handling...
        // }
    }

    internal void ApplyChanges( IActivityMonitor monitor )
    {
        using var _ = monitor.OpenInfo( $"Recomputing 'ck-gen/assets'." );
        bool success = true;
        var f = new FinalResourceAssetSet( isPartialSet: false );
        foreach( var p in _packages )
        {
            success &= p.ApplyResourceAssetSet( monitor, f );
        }
        if( success )
        {
            success &= _state.CKGenTransform.LoadAssets( monitor,
                                                         defaultTargetPath: default,
                                                         out var appAssets,
                                                         "assets" );
            if( appAssets != null )
            {
                f.Add( monitor, appAssets );
            }
            if( success )
            {
                SetFinal( monitor, f.Final.Assets );
                _isDirty = false;
            }
        }
    }

    void SetFinal( IActivityMonitor monitor, IReadOnlyDictionary<NormalizedPath, ResourceAsset> newFinal )
    {
        Throw.DebugAssert( _final != null );

        List<NormalizedPath>? toRemove = null;
        List<(string, NormalizedPath)>? fileToCopy = null;
        List<(ResourceLocator, NormalizedPath)>? resToCopy = null;

        // First, updates the current _final with the new one.
        foreach( var (path,asset) in newFinal )
        {
            if( _final.TryGetValue( path, out var already ) )
            {
                if( already.Origin == asset.Origin )
                {
                    // Same resource for the target path. We must only handle local file
                    // changes (disappeared or modified).
                    if( already.LocalPath != null )
                    {
                        var lwt = File.GetLastWriteTimeUtc( already.LocalPath );
                        if( lwt != already.LastWriteTime )
                        {
                            if( lwt == FileUtil.MissingFileLastWriteTimeUtc )
                            {
                                HandleUnexistingFile( monitor, ref toRemove, path, already.LocalPath );
                                _final.Remove( path );
                            }
                            else
                            {
                                fileToCopy ??= new List<(string, NormalizedPath)>();
                                fileToCopy.Add( (already.LocalPath, path) );
                                _final[path] = already with { LastWriteTime = lwt };
                            }
                        }
                    }
                }
                else
                {
                    // Target path is mapped to another resource.
                    bool updateFinal = true;
                    var updated = FinalAsset.ToFinal( asset.Origin );
                    if( updated.LocalPath != null )
                    {
                        // If the updated resource file doesn't exist anymore, we remove
                        // it from _final and adds the target path to the remove list.
                        if( !updated.Exists )
                        {
                            HandleUnexistingFile( monitor, ref toRemove, path, updated.LocalPath );
                            _final.Remove( path );
                            updateFinal = false;
                        }
                        else
                        {
                            fileToCopy ??= new List<(string, NormalizedPath)>();
                            fileToCopy.Add( (updated.LocalPath, path) );
                        }
                    }
                    else
                    {
                        resToCopy ??= new List<(ResourceLocator, NormalizedPath)>();
                        resToCopy.Add( (asset.Origin, path) );
                    }
                    if( updateFinal ) _final[path] = updated;
                }
            }
        }

        // Second, remove any previous _final path that are not mapped.
        foreach( var path in _final.Keys )
        {
            if( !newFinal.ContainsKey( path ) )
            {
                toRemove ??= new List<NormalizedPath>();
                toRemove.Add( path );
            }
        }
        var ckGenAssets = _state.Paths.CKGenPath + "assets" + Path.DirectorySeparatorChar;
        if( resToCopy != null ) CopyFromResources( monitor, resToCopy, ckGenAssets );
        if( fileToCopy != null ) CopyFromFiles( monitor, fileToCopy, ckGenAssets );
        if( toRemove != null ) RemoveFinalFiles( monitor, _final, toRemove, ckGenAssets );

        static void HandleUnexistingFile( IActivityMonitor monitor,
                                          ref List<NormalizedPath>? toRemove,
                                          NormalizedPath path,
                                          string filePath )
        {
            monitor.Warn( $"File '{filePath}' doesn't exist anymore. Removing 'ck-gen/assets/{path}'." );
            toRemove ??= new List<NormalizedPath>();
            toRemove.Add( path );
        }

        static void CopyFromResources( IActivityMonitor monitor,
                                       List<(ResourceLocator, NormalizedPath)> resToCopy,
                                       string ckGenAssets )
        {
            using( monitor.OpenInfo( $"Updating {resToCopy.Count} files from assembly resources." ) )
            {
                monitor.Debug( $"Updating: {resToCopy.Select( f => f.Item2.Path ).Concatenate()}." );
                foreach( var (r, path) in resToCopy )
                {
                    try
                    {
                        using( var s = File.Create( ckGenAssets + path.Path ) )
                        {
                            r.WriteStream( s );
                        }
                    }
                    catch( Exception ex )
                    {
                        monitor.Error( $"While extracting '{r}' to '{path}'.", ex );
                    }
                }
            }
        }

        static void CopyFromFiles( IActivityMonitor monitor,
                                   List<(string, NormalizedPath)> fileToCopy,
                                   string ckGenAssets )
        {
            using( monitor.OpenInfo( $"Copying {fileToCopy.Count} files." ) )
            {
                monitor.Debug( $"Updating: {fileToCopy.Select( f => f.Item2.Path ).Concatenate()}." );
                foreach( var (filePath, path) in fileToCopy )
                {
                    try
                    {
                        File.Copy( filePath, ckGenAssets + path.Path, overwrite: true );
                    }
                    catch( Exception ex )
                    {
                        monitor.Error( $"While copying '{filePath}' to '{path}'.", ex );
                    }
                }
            }
        }

        static void RemoveFinalFiles( IActivityMonitor monitor,
                                      Dictionary<NormalizedPath, FinalAsset> final,
                                      List<NormalizedPath> toRemove,
                                      string ckGenAssets )
        {
            using( monitor.OpenInfo( $"Removing {toRemove.Count} asset files." ) )
            {
                monitor.Debug( $"Removing: {toRemove.Select( f => f.Path ).Concatenate()}." );
                foreach( var path in toRemove )
                {
                    try
                    {
                        File.Delete( ckGenAssets + path.Path );
                    }
                    catch( Exception ex )
                    {
                        monitor.Error( $"While deleting '{path}'.", ex );
                    }
                    final.Remove( path );
                }
            }
        }
    }

}
