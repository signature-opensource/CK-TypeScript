using CK.BinarySerialization;
using CK.Core;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CK.TypeScript.LiveEngine;

sealed class LiveState
{
    readonly ResSpaceData _spaceData;
    readonly ImmutableArray<ILiveUpdater> _updaters;
    readonly string _ckGenAppPath;
    readonly string _watchRoot;

    internal LiveState( ResSpaceData spaceData, ImmutableArray<ILiveUpdater> updaters )
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

    public ResSpaceData SpaceData => _spaceData;

    public string CKGenAppPath => _ckGenAppPath;

    public string WatchRoot => _watchRoot;

    /// <summary>
    /// Called by the file watcher.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="changed">The changed event.</param>
    public void OnChange( IActivityMonitor monitor, PathChangedEvent changed )
    {
        foreach( var u in _updaters )
        {
            if( u.OnChange( monitor, changed ) )
            {
                break;
            }
        }
    }

    /// <summary>
    /// Applies any changes.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    public void ApplyChanges( IActivityMonitor monitor )
    {
        var success = true;
        foreach( var u in _updaters )
        {
            try
            {
                success &= u.ApplyChanges( monitor );
            }
            catch( Exception ex )
            {
                monitor.Error( ex );
                success = false;
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
                return r.GetResult(); 
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
            if( v != ResSpace.CurrentVersion )
            {
                monitor.Error( $"Invalid version '{v}', expected '{ResSpace.CurrentVersion}'." );
                return null;
            }
            var spaceData = d.ReadObject<ResSpaceData>();
            if( spaceData.LiveStatePath + ResSpace.LiveStateFileName != liveStateFilePath )
            {
                monitor.Error( $"Invalid paths. Expected '{ResSpace.LiveStateFileName}' to be in '{spaceData.LiveStatePath}'." );
                return null;
            }
            bool success = true;
            var updaters = new ILiveUpdater[r.ReadNonNegativeSmallInt32()];
            for( int i = 0; i < updaters.Length; ++i )
            {
                var t = d.ReadTypeInfo().ResolveLocalType();
                var o = t.InvokeMember( nameof( ILiveResourceSpaceHandler.ReadLiveState ),
                                        BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
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

