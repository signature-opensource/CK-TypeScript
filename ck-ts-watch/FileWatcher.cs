using CK.TypeScript.LiveEngine;
using System.ComponentModel;
using System.IO;
using System.Threading.Channels;

sealed class FileWatcher
{
    FileSystemWatcher? _fileSystemWatcher;
    volatile bool _closed;
    readonly object _lock = new();
    readonly string _watchRoot;
    readonly ChannelWriter<object> _output;
    readonly IFileEventFilter _fileFilter;

    internal FileWatcher( string watchRoot,
                          ChannelWriter<object> output,
                          IFileEventFilter fileFilter )
    {
        _watchRoot = watchRoot;
        _output = output;
        _fileFilter = fileFilter;
        CreateFileSystemWatcher();
    }

    public void Dispose()
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
        var o = _fileFilter.GetChange( e.OldFullPath );
        if( o != null )
        {
            _output.TryWrite( o );
        }
        o = _fileFilter.GetChange( e.FullPath );
        if( o != null )
        {
            _output.TryWrite( o );
        }
    }

    void WatcherChangeHandler( object sender, FileSystemEventArgs e )
    {
        if( _closed ) return;
        var o = _fileFilter.GetChange( e.FullPath );
        if( o != null )
        {
            _output.TryWrite( o );
        }
    }

    void CreateFileSystemWatcher()
    {
        lock( _lock )
        {
            if( _fileSystemWatcher != null )
            {
                DisposeInnerWatcher();
            }

            _fileSystemWatcher = new FileSystemWatcher( _watchRoot )
            {
                IncludeSubdirectories = true
            };
            _fileSystemWatcher.Created += WatcherChangeHandler;
            _fileSystemWatcher.Deleted += WatcherChangeHandler;
            _fileSystemWatcher.Changed += WatcherChangeHandler;
            _fileSystemWatcher.Renamed += WatcherRenameHandler;
            _fileSystemWatcher.Error += WatcherErrorHandler;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }
    }

    void DisposeInnerWatcher()
    {
        if( _fileSystemWatcher != null )
        {
            _fileSystemWatcher.EnableRaisingEvents = false;

            _fileSystemWatcher.Created -= WatcherChangeHandler;
            _fileSystemWatcher.Deleted -= WatcherChangeHandler;
            _fileSystemWatcher.Changed -= WatcherChangeHandler;
            _fileSystemWatcher.Renamed -= WatcherRenameHandler;
            _fileSystemWatcher.Error -= WatcherErrorHandler;

            _fileSystemWatcher.Dispose();
        }
    }
}
