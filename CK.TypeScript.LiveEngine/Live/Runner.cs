using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

sealed class Runner
{
    readonly LiveState _liveState;
    readonly Channel<object> _channel;
    readonly FileWatcher _primary;
    readonly FileWatcher? _secondary;
    readonly Timer _timer;

    public Runner( LiveState liveState, CKGenTransformFilter stateFilesFilter )
    {
        _liveState = liveState;
        var needPrimaryOnly = liveState.Paths.CKGenTransformPath.StartsWith( liveState.WatchRootPath );
        _channel = Channel.CreateUnbounded<object>( new UnboundedChannelOptions() { SingleReader = true, SingleWriter = needPrimaryOnly } );
        var localPackagesFilter = new LocalPackagesFilter( liveState.LocalPackages );
        _primary = new FileWatcher( liveState.WatchRootPath,
                                    _channel.Writer,
                                    needPrimaryOnly
                                        ? new BothFilesFilter( localPackagesFilter, stateFilesFilter )
                                        : localPackagesFilter );
        if( !needPrimaryOnly )
        {
            _secondary = new FileWatcher( liveState.Paths.CKGenTransformPath, _channel.Writer, stateFilesFilter );
        }
        _timer = new Timer( OnTimer );
    }

    void OnTimer( object? state )
    {
        _channel.Writer.TryWrite( _timer );
    }

    public async Task RunAsync( IActivityMonitor monitor, System.Threading.CancellationToken cancellation )
    {
        try
        {
            object e;
            while( (e = await _channel.Reader.ReadAsync( cancellation )) != CKGenTransformFilter.PrimaryStateFileChanged )
            {
                switch( e )
                {
                    case Exception ex:
                        monitor.Warn( $"File system watcher error.", ex );
                        break;
                    case ChangedEvent p:
                        monitor.Debug( $"Change: '{p.Package}' {p.SubPath}" );
                        _liveState.OnChange( monitor, p.Package, p.SubPath );
                        if( !_timer.Change( 80, Timeout.Infinite ) )
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
            monitor.Info( "Primary state has been modified." );
        }
        catch( OperationCanceledException c ) when (c.CancellationToken == cancellation )
        {
            monitor.Info( "Stopped signal received." );
        }
        catch( Exception ex )
        {
            monitor.Error( ActivityMonitor.Tags.ToBeInvestigated, "Internal error.", ex );
        }
        _primary.Dispose();
        _secondary?.Dispose();
    }
}
