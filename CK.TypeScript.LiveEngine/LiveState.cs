using CK.BinarySerialization;
using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CK.TypeScript.LiveEngine;

public sealed class LiveState
{
    readonly ResourceSpaceData _spaceData;
    readonly ImmutableArray<ILiveUpdater> _updaters;
    readonly string _ckGenAppPath;
    readonly string _watchRoot;

    internal LiveState( ResourceSpaceData spaceData, ImmutableArray<ILiveUpdater> updaters )
    {
        // Are we internal (DebugAssert) or public (CheckArgument) here?
        // Consider that this may be called out of control.
        Throw.CheckArgument( "TypeScript always has the <App> ck-gen-app/ folder.", spaceData.AppPackage.Resources.LocalPath != null );
        Throw.CheckArgument( spaceData.WatchRoot != null );
        _spaceData = spaceData;
        _updaters = updaters;
        _ckGenAppPath = spaceData.AppPackage.Resources.LocalPath;
        _watchRoot = spaceData.WatchRoot;
    }

    public ResourceSpaceData SpaceData => _spaceData;

    public string CKGenPath => _spaceData.CKGenPath;

    public string CKGenAppPath => _ckGenAppPath;

    public string WatchRoot => _watchRoot;

    /// <summary>
    /// Called by the file watcher.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resources">The resources folder.</param>
    /// <param name="subPath">The path in the resource folder.</param>
    public void OnChange( IActivityMonitor monitor, IResPackageResources resources, string subPath )
    {
        foreach( var u in _updaters )
        {
            if( u.OnChange( monitor, resources, subPath ) )
            {
                break;
            }
        }
    }

    sealed class UpdateInstaller : SimpleFileSystemInstaller
    {
        public readonly List<string> Written;

        public UpdateInstaller( string targetPath )
            : base( targetPath )
        {
            Written = new List<string>();
        }

        protected override void OnWrite( string path ) => Written.Add( path );
    }

    /// <summary>
    /// Applies any changes.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    public void ApplyChanges( IActivityMonitor monitor )
    {
        var success = true;
        var installer = new UpdateInstaller( _spaceData.CKGenPath );
        foreach( var u in _updaters )
        {
            try
            {
                success &= u.ApplyChanges( monitor, installer );
            }
            catch( Exception ex )
            {
                monitor.Error( ex );
                success = false;
            }
        }
        using( monitor.OpenInfo( $"Updated {installer.Written.Count} files." ) )
        {
            monitor.Trace( installer.Written.Concatenate() );
            if( !success )
            {
                monitor.CloseGroup( "Failed!" );
            }
        }
    }

    public static async Task<LiveState> WaitForStateAsync( IActivityMonitor monitor,
                                                           string liveStateFilePath,
                                                           CancellationToken cancellation )
    {
        var liveState = Load( monitor, liveStateFilePath );
        while( liveState == null )
        {
            await Task.Delay( 500, cancellation );
            liveState = Load( monitor, liveStateFilePath );
        }
        return liveState;
    }

    static LiveState? Load( IActivityMonitor monitor, string liveStateFilePath )
    {
        if( !File.Exists( liveStateFilePath ) ) return null;
        try
        {
            using( var stream = File.OpenRead( liveStateFilePath ) )
            {
                var r = BinaryDeserializer.Deserialize( stream, new BinaryDeserializerContext(),
                    d => ReadLiveState( monitor, d, liveStateFilePath ) );
                return r.IsValid ? r.GetResult() : null; 
            }
        }
        catch( Exception e )
        {
            monitor.Error( e );
            return null;
        }

        static LiveState? ReadLiveState( IActivityMonitor monitor, IBinaryDeserializer d, string liveStateFilePath )
        {
            var r = d.Reader;
            byte v = r.ReadByte();
            if( v != ResourceSpace.CurrentVersion )
            {
                monitor.Error( $"Invalid version '{v}', expected '{ResourceSpace.CurrentVersion}'." );
                return null;
            }
            var spaceData = d.ReadObject<ResourceSpaceData>();
            if( spaceData.LiveStatePath + ResourceSpace.LiveStateFileName != liveStateFilePath )
            {
                monitor.Error( $"Invalid paths. Expected '{ResourceSpace.LiveStateFileName}' to be in '{spaceData.LiveStatePath}'." );
                return null;
            }
            bool success = true;
            var updaters = new ILiveUpdater[r.ReadNonNegativeSmallInt32()];
            for( int i = 0; i < updaters.Length; ++i )
            {
                var t = d.ReadTypeInfo().ResolveLocalType();
                var o = t.InvokeMember( nameof( ILiveResourceSpaceHandler.ReadLiveState ),
                                        BindingFlags.Public | BindingFlags.Static,
                                        null,
                                        null,
                                        [monitor, spaceData, d] );
                if( o is not ILiveUpdater updater )
                {
                    monitor.Error( $"Unable to restore a Live updater from '{t:N}'." );
                    success = false;
                }
                else
                {
                    updaters[i] = updater;
                }
            }
            return success
                        ? new LiveState( spaceData, updaters.ToImmutableArray() )
                        : null;
        }

    }

}

