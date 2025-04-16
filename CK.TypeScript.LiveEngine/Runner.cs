using CK.Core;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CK.TypeScript.LiveEngine;

public static class Runner
{
    /// <summary>
    /// Runs until <paramref name="cancellation"/> is signaled.
    /// <para>
    /// This goes async after the first failed attempt to load the state or when waiting for a file change
    /// and this is intended: if the Live State file is initially available, the watcher is setup (and waits
    /// for file changes) when this method returns its Task. 
    /// </para>
    /// </summary>
    /// <param name="loopMonitor">The dedicated monitor to use by this loop. Should not be used outside.</param>
    /// <param name="liveStateFilePath">The live state path (must end with <see cref="ResSpace.LiveStateFileName"/>).</param>
    /// <param name="cancellation">The cancellation token that will stop the loop. <see cref="CancellationToken.CanBeCanceled"/> must be true.</param>
    /// <returns>The running task.</returns>
    public static async Task RunAsync( ActivityMonitor loopMonitor,
                                       string liveStateFilePath,
                                       uint debounceMs,
                                       CancellationToken cancellation )
    {
        Throw.CheckArgument( liveStateFilePath.EndsWith( ResSpace.LiveStateFileName ) );
        Throw.CheckArgument( cancellation.CanBeCanceled );

        CKGenAppFilter? stateFilesFilter = null;
        while( !cancellation.IsCancellationRequested )
        {
            loopMonitor.Info( $"""
                    Waiting for file:
                    -> {liveStateFilePath}
                    """ );
            var liveState = await LiveState.WaitForStateAsync( loopMonitor, liveStateFilePath, cancellation );
            loopMonitor.Info( "Running watcher." );
            stateFilesFilter ??= new CKGenAppFilter( liveStateFilePath, liveState );
            var runner = new RunnerState( liveState, stateFilesFilter, debounceMs );
            await runner.RunAsync( loopMonitor, cancellation );
            loopMonitor.Info( "Watcher stopped." );
        }
    }

    sealed class RunnerState
    {
        readonly LiveState _liveState;
        readonly uint _debounceMs;
        readonly Channel<object?> _channel;
        readonly FileWatcher _primary;
        readonly FileWatcher? _secondary;
        readonly Timer _timer;

        public RunnerState( LiveState liveState, CKGenAppFilter stateFilesFilter, uint debounceMs )
        {
            _liveState = liveState;
            _debounceMs = debounceMs;
            var needPrimaryOnly = liveState.CKGenAppPath.StartsWith( liveState.WatchRoot );
            _channel = Channel.CreateUnbounded<object?>( new UnboundedChannelOptions() { SingleReader = true, SingleWriter = needPrimaryOnly } );
            var localPackagesFilter = new LocalPackagesFilter( liveState.SpaceData.LocalPackages );
            _primary = new FileWatcher( liveState.WatchRoot,
                                        _channel.Writer,
                                        needPrimaryOnly
                                            ? new BothFilesFilter( localPackagesFilter, stateFilesFilter )
                                            : localPackagesFilter );
            if( !needPrimaryOnly )
            {
                _secondary = new FileWatcher( liveState.CKGenAppPath, _channel.Writer, stateFilesFilter );
            }
            _timer = new Timer( OnTimer );
        }

        void OnTimer( object? state )
        {
            _channel.Writer.TryWrite( _timer );
        }

        public async Task RunAsync( IActivityMonitor monitor, CancellationToken cancellation )
        {
            using var regCancellation = cancellation.UnsafeRegister( w => Unsafe.As<ChannelWriter<object?>>( w! ).TryWrite( null ), _channel.Writer );
            try
            {
                for(; ; )
                {
                    object? e = await _channel.Reader.ReadAsync( CancellationToken.None );
                    if( e == null )
                    {
                        monitor.Info( "Stopped signal received." );
                        break;
                    }
                    if( e == CKGenAppFilter.PrimaryStateFileChanged )
                    {
                        monitor.Info( "Primary state has been modified." );
                        break;
                    }
                    switch( e )
                    {
                        case Exception ex:
                            monitor.Warn( $"File system watcher error.", ex );
                            break;
                        case ChangedEvent p:
                            monitor.Debug( $"Change: '{p.Resources.Package}' {p.SubPath}" );
                            _liveState.OnChange( monitor, p.Resources, p.SubPath );
                            if( !_timer.Change( _debounceMs, unchecked((uint)-1) ) )
                            {
                                monitor.Warn( ActivityMonitor.Tags.ToBeInvestigated, "Failed to update Timer duetime." );
                            }
                            break;
                        case Timer:
                            using( monitor.OpenDebug( "Applying changes..." ) )
                            {
                                _liveState.ApplyChanges( monitor );
                            }
                            break;
                    }
                }
            }
            catch( Exception ex )
            {
                monitor.Error( ActivityMonitor.Tags.ToBeInvestigated, "Internal error.", ex );
            }
            _primary.Dispose();
            _secondary?.Dispose();
        }
    }

}


