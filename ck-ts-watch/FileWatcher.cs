using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

sealed class Runner
{
    readonly LiveState _liveState;
    readonly Channel<object?> _channel;
    readonly FileWatcher _primary;
    readonly FileWatcher? _secondary;
    readonly Task _running;

    public Runner( LiveState liveState )
    {
        _liveState = liveState;
        var needPrimaryOnly = liveState.WatchRoot.StartsWith( liveState.LoadFolder );
        _channel = Channel.CreateUnbounded<object?>( new UnboundedChannelOptions() { SingleReader = true, SingleWriter = needPrimaryOnly } );
        _primary = new FileWatcher( liveState.WatchRoot, _channel.Writer );
        if( !needPrimaryOnly )
        {
            _secondary = new FileWatcher( liveState.LoadFolder, _channel.Writer );
        }
        _running = Task.Run( RunAsync );
    }

    public Task RunningTask => _running;

    async Task RunAsync()
    {
        var monitor = new ActivityMonitor( "ck-ts-watch Agent." );
        monitor.Output.RegisterClient( new ColoredActivityMonitorConsoleClient() );
        object? message = null;
        while( (message = await _channel.Reader.ReadAsync()) != null )
        {

        }
        monitor.MonitorEnd();
    }
}


internal sealed class FileWatcher
{
    FileSystemWatcher? _fileSystemWatcher;
    volatile bool _closed;
    readonly object _createLock = new();
    readonly string _watchRoot;
    readonly ChannelWriter<object?> _output;
    readonly string? _keepAliveFile;

    internal FileWatcher( string watchRoot,
                          ChannelWriter<object?> output,
                          string? keepAliveFile )
    {
        _watchRoot = watchRoot;
        _output = output;
        _keepAliveFile = keepAliveFile;
        CreateFileSystemWatcher();
    }

    public void Close()
    {
        _closed = true;
        DisposeInnerWatcher();
    }

    void WatcherErrorHandler( object sender, ErrorEventArgs e )
    {
        if( _closed ) return;
        var exception = e.GetException();
        if( exception is not Win32Exception )
        {
            // Recreate the watcher if it is a recoverable error.
            CreateFileSystemWatcher();
        }
        _output.TryWrite( exception );
    }

    void WatcherRenameHandler( object sender, RenamedEventArgs e )
    {
        if( _closed ) return;
        if( e.OldFullPath == _keepAliveFile )
        {
            _output.TryWrite( null );
        }
    }

    void WatcherDeletedHandler( object sender, FileSystemEventArgs e )
    {
        if( _closed ) return;
        var p = e.FullPath;
        if( p == _keepAliveFile )
        {
            _output.TryWrite( null );
        }
        else
        {
            int idxRes = p.IndexOf( "/Res/" );
            if( idxRes > 0 )
            {

            }
        }
    }

    void WatcherChangeHandler( object sender, FileSystemEventArgs e )
    {
        if( _closed ) return;
    }

    void WatcherAddedHandler( object sender, FileSystemEventArgs e )
    {
        if( _closed ) return;
    }

    private void CreateFileSystemWatcher()
    {
        lock( _createLock )
        {
            bool enableEvents = false;

            if( _fileSystemWatcher != null )
            {
                enableEvents = _fileSystemWatcher.EnableRaisingEvents;

                DisposeInnerWatcher();
            }

            _fileSystemWatcher = new FileSystemWatcher( _watchRoot )
            {
                IncludeSubdirectories = true
            };

            _fileSystemWatcher.Created += WatcherAddedHandler;
            _fileSystemWatcher.Deleted += WatcherDeletedHandler;
            _fileSystemWatcher.Changed += WatcherChangeHandler;
            _fileSystemWatcher.Renamed += WatcherRenameHandler;
            _fileSystemWatcher.Error += WatcherErrorHandler;

            _fileSystemWatcher.EnableRaisingEvents = enableEvents;
        }
    }

    private void DisposeInnerWatcher()
    {
        if( _fileSystemWatcher != null )
        {
            _fileSystemWatcher.EnableRaisingEvents = false;

            _fileSystemWatcher.Created -= WatcherAddedHandler;
            _fileSystemWatcher.Deleted -= WatcherDeletedHandler;
            _fileSystemWatcher.Changed -= WatcherChangeHandler;
            _fileSystemWatcher.Renamed -= WatcherRenameHandler;
            _fileSystemWatcher.Error -= WatcherErrorHandler;

            _fileSystemWatcher.Dispose();
        }
    }
}
