using CK.BinarySerialization;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace CK.Core;

/// <summary>
/// Final production initiated by a <see cref="ResourceSpaceCollectorBuilder"/>.
/// Once available, all registered <see cref="FolderHandlers"/> and <see cref="FileHandlers"/>
/// have been successfully initialized.
/// </summary>
public sealed class ResourceSpace
{
    public const string LiveStateFileName = "LiveState.dat";
    public static int CurrentVersion = 0;

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
    /// Writes the live state.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="ckWatchFolderPath">
    /// Defaults to the "<see cref="ResourceSpaceCollector.AppResourcesLocalPath"/>/.ck-watch" (that is <see cref="ResourceSpaceData.AppPackage"/>'s
    /// <see cref="ResPackage.LocalPath"/>) when AppResourcesLocalPath is defined.
    /// If both this parameter and AppResourcesLocalPath are null, live state is not saved even if there are local packages.
    /// </param>
    public bool WriteLiveState( IActivityMonitor monitor, string? ckWatchFolderPath = null )
    {
        ckWatchFolderPath ??= _data.AppPackage.LocalPath;
        if( ckWatchFolderPath == null )
        {
            monitor.Warn( """
                No AppResourcesLocalPath has been set and no target watch folder has been specified.
                Skipping Live state generation.
                """ );
            return true;
        }
        if( !Path.IsPathFullyQualified( ckWatchFolderPath )
            || File.Exists( ckWatchFolderPath )
            || !ckWatchFolderPath.EndsWith( Path.DirectorySeparatorChar ) )
        {
            monitor.Error( $"""
                    Invalid ck-watch state folder. It must not be a file, must be a fully qualified folder path that ends with '{Path.DirectorySeparatorChar}':
                    {ckWatchFolderPath}
                    """ );
            return false;
        }
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
            s.Writer.Write( (byte)CurrentVersion );
            s.Writer.Write( ckWatchFolderPath );
            s.Writer.Write( _data.WatchRoot );
            s.Writer.WriteNonNegativeSmallInt32( liveHandlers.Count );
            foreach( var h in liveHandlers )
            {
                success &= h.WriteLiveState( monitor, s, ckWatchFolderPath );
            }
        }
        return success;
    }
    
}
