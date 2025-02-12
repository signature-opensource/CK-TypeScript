using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

sealed class Runner
{
    readonly LiveState _liveState;
    readonly CKGenTransformFilter _stateFilesFilter;
    readonly Channel<object> _channel;
    readonly FileWatcher _primary;
    readonly FileWatcher? _secondary;

    public Runner( LiveState liveState, CKGenTransformFilter stateFilesFilter )
    {
        _liveState = liveState;
        _stateFilesFilter = stateFilesFilter;
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
    }

    public async Task RunAsync( IActivityMonitor monitor )
    {
        object e;
        while( (e = await _channel.Reader.ReadAsync()) != CKGenTransformFilter.PrimaryStateFileChanged ) 
        {
            switch( e )
            {
                case Exception ex:
                    monitor.Warn( $"File system watcher error.", ex );
                    break;
                case ChangedEvent p:
                    _liveState.OnChange( monitor, p.Package, p.SubPath );
                    break;
            }
        }
        monitor.Info( "Watcher stopped." );
        _primary.Dispose();
        _secondary?.Dispose();
    }
}
