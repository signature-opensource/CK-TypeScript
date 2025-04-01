using CK.BinarySerialization;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace CK.Core;

/// <summary>
/// Final production initiated by a <see cref="ResourceSpaceCollectorBuilder"/>.
/// Once available, all registered <see cref="FolderHandlers"/> and <see cref="FileHandlers"/>
/// have been successfully initialized.
/// </summary>
public sealed class ResourceSpace
{
    /// <summary>
    /// The Live state file name.
    /// </summary>
    public const string LiveStateFileName = "LiveState.dat";

    /// <summary>
    /// The current serialization format version.
    /// </summary>
    public static readonly byte CurrentVersion = 0;

    readonly ResourceSpaceData _data;
    readonly ImmutableArray<ResourceSpaceFolderHandler> _folderHandlers;
    readonly ImmutableArray<ResourceSpaceFileHandler> _fileHandlers;
    readonly ResourceSpaceFileHandler.FolderExclusion _folderExclusion;
    
    internal ResourceSpace( ResourceSpaceData data,
                            ImmutableArray<ResourceSpaceFolderHandler> folderHandlers,
                            ImmutableArray<ResourceSpaceFileHandler> fileHandlers )
    {
        _data = data;
        _folderHandlers = folderHandlers;
        _fileHandlers = fileHandlers;
        _folderExclusion = new ResourceSpaceFileHandler.FolderExclusion( folderHandlers );
    }

    /// <summary>
    /// Gets the resources data.
    /// </summary>
    public ResourceSpaceData ResourceSpaceData => _data;

    /// <summary>
    /// Gets the successfully initialized <see cref="ResourceSpaceFolderHandler"/>.
    /// </summary>
    public ImmutableArray<ResourceSpaceFolderHandler> FolderHandlers => _folderHandlers;

    /// <summary>
    /// Gets the successfully initialized <see cref="ResourceSpaceFileHandler"/>.
    /// </summary>
    public ImmutableArray<ResourceSpaceFileHandler> FileHandlers => _fileHandlers;

    // Called by the ResourceSpaceBuilder.Build().
    internal bool Initialize( IActivityMonitor monitor )
    {
        bool success = true;
        foreach( var h in _folderHandlers )
        {
            success &= h.Initialize( monitor, _data );
        }
        foreach( var h in _fileHandlers )
        {
            success &= h.Initialize( monitor, _data, _folderExclusion );
        }
        return success;
    }

    /// <summary>
    /// Generates resources into the <see cref="ResourceSpaceData.CKGenPath"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool Install( IActivityMonitor monitor )
    {
        var installer = InitialFileInstaller.Create( monitor, _data.CKGenPath );
        if( installer == null ) return false;

        bool success = true;
        if( _data.CKWatchFolderPath != ResourceSpaceCollector.NoLiveState )
        {
            success &= ClearLiveState( monitor, _data.CKWatchFolderPath );
        }
        foreach( var f in _folderHandlers )
        {
            success &= f.Install( monitor, installer );
        }
        foreach( var f in _fileHandlers )
        {
            success &= f.Install( monitor, installer );
        }

        installer.Cleanup( monitor, success );

        if( success && _data.CKWatchFolderPath != ResourceSpaceCollector.NoLiveState )
        {
            if( _data.CKWatchFolderPath == ResourceSpaceCollector.NoLiveState )
            {
                monitor.Warn( """
                No AppResourcesLocalPath has been set and no target watch folder has been specified.
                Skipping Live state generation.
                """ );
            }
            success &= WriteLiveState( monitor, _data.CKWatchFolderPath );
        }
        return success;
    }

    static bool ClearLiveState( IActivityMonitor monitor, string ckWatchFolderPath )
    {
        Throw.DebugAssert( ckWatchFolderPath.EndsWith( Path.DirectorySeparatorChar ) );
        if( File.Exists( ckWatchFolderPath ) )
        {
            monitor.Error( $"""
                    Invalid ck-watch state folder. It must not be a file':
                    {ckWatchFolderPath}
                    """ );
            return false;
        }
        var stateFile = ckWatchFolderPath + LiveStateFileName;
        if( File.Exists( stateFile ) )
        {
            int retryCount = 0;
            retry:
            try
            {
                File.Delete( stateFile );
                monitor.Trace( $"Deleted state file '{stateFile}'." );
            }
            catch( Exception ex )
            {
                if( ++retryCount < 3 )
                {
                    monitor.Warn( $"While clearing state file '{stateFile}'.", ex );
                    Thread.Sleep( retryCount * 100 );
                    goto retry;
                }
                monitor.Warn( $"Unable to delete state file '{stateFile}'.", ex );
            }
        }
        return true;
    }

    bool WriteLiveState( IActivityMonitor monitor, string ckWatchFolderPath )
    {
        if( _data.WatchRoot == null )
        {
            monitor.Info( """
                No local package exist and no AppResourcesLocalPath has been set.
                Skipping Live state generation.
                """ );
            return true;
        }
        var liveHandlers = _folderHandlers.OfType<ILiveResourceSpaceHandler>()
                                          .Concat( _fileHandlers.OfType<ILiveResourceSpaceHandler>() )
                                          .ToList();
        if( liveHandlers.Count == 0 )
        {
            monitor.Info( $"""
                No Live resource handlers exist in the {_folderHandlers.Length} folders and {_fileHandlers.Length} files resource handlers.
                Skipping Live state generation.
                """ );
            return true;
        }
        using var _ = monitor.OpenInfo( $"Saving ck-watch live state. Watch root is '{_data.WatchRoot}'." );
        if( !Directory.Exists( ckWatchFolderPath ) )
        {
            monitor.Info( $"Creating '{ckWatchFolderPath}' with '.gitignore' all." );
            Directory.CreateDirectory( ckWatchFolderPath );
            File.WriteAllText( ckWatchFolderPath + ".gitignore", "*" );
        }
        // Don't store the BinarySerializerContext for reuse here: this is a one-shot.
        bool success = true;
        using( var streamLive = new FileStream( ckWatchFolderPath + LiveStateFileName, FileMode.Create ) )
        using( var s = BinarySerializer.Create( streamLive, new BinarySerializerContext() ) )
        {
            s.Writer.Write( CurrentVersion );
            s.WriteObject( _data );
            s.Writer.WriteNonNegativeSmallInt32( liveHandlers.Count );
            foreach( var h in liveHandlers )
            {
                s.WriteTypeInfo( h.GetType() );
                success &= h.WriteLiveState( monitor, s, ckWatchFolderPath );
            }
        }
        return success;
    }

}
