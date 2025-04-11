using CK.BinarySerialization;
using System;
using System.IO;
using System.Linq;

namespace CK.Core;

public partial class LocalesResourceHandler : ILiveResourceSpaceHandler
{
    /// <summary>
    /// Live update is currently supported only when installing on the file system:
    /// the <see cref="FileSystemInstaller.TargetPath"/> is serialized in the live state
    /// and a basic <see cref="FileSystemInstaller"/> is deserialized to be the installer
    /// on the live side.
    /// </summary>
    public bool DisableLiveUpdate => Installer is not FileSystemInstaller;

    bool ILiveResourceSpaceHandler.WriteLiveState( IActivityMonitor monitor, IBinarySerializer s, string ckWatchFolderPath )
    {
        Throw.DebugAssert( "Otherwise LiveState would have been disabled.", Installer is FileSystemInstaller );
        s.Writer.Write( ((FileSystemInstaller)Installer).TargetPath );
        s.Writer.Write( string.Join( ',', _cache.ActiveCultures.AllActiveCultures.Select( c => c.Culture.Name ) ) );
        s.Writer.Write( RootFolderName );
        s.Writer.WriteNonNegativeSmallInt32( (int)_installOption );
        return true;
    }

    public static ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResSpaceData data, IBinaryDeserializer d )
    {
        var installer = new FileSystemInstaller( d.Reader.ReadString() );
        var cultures = d.Reader.ReadString().Split( ',' ).Select( NormalizedCultureInfo.EnsureNormalizedCultureInfo );
        var activeCultures = new ActiveCultureSet( cultures );
        var rootFolderName = d.Reader.ReadString();
        var options = (InstallOption)d.Reader.ReadNonNegativeSmallInt32();
        var handler = new LocalesResourceHandler( null, data.ResPackageDataCache, rootFolderName, activeCultures, options );
        return new LiveUpdater( handler, installer, data );
    }

    sealed class LiveUpdater : ILiveUpdater
    {
        readonly LocalesResourceHandler _handler;
        readonly FileSystemInstaller _installer;
        readonly ResSpaceData _data;
        readonly string[] _activeNames;
        bool _hasChanged;

        public LiveUpdater( LocalesResourceHandler handler, FileSystemInstaller installer, ResSpaceData data )
        {
            _handler = handler;
            _installer = installer;
            _data = data;
            _activeNames = handler._cache.ActiveCultures.AllActiveCultures
                                                        .Select( c => Path.DirectorySeparatorChar + c.Culture.Name )
                                                        .ToArray();
        }

        bool ILiveUpdater.OnChange( IActivityMonitor monitor, IResPackageResources resources, string filePath )
        {
            // This first filter allows us to exit quickly.
            if( !IsFileInRootFolder( _handler.RootFolderName, filePath, out ReadOnlySpan<char> localFile ) )
            {
                return false;
            }
            // If localFile is empty, something happened to the whole IResPackageResources.LocalPath
            // or to our RootFolderName.
            // Otherwise, it must be a ".jsonc" file and its name must be one of the active culture names.
            // Invalidate all the cache.
            if( localFile.Length == 0 || (localFile.EndsWith( ".jsonc" ) && IsActiveCultureFile( localFile[..^6] ) ) )
            {
                _hasChanged = true;
            }
            else
            {
                monitor.Trace( $"'/{_handler.RootFolderName}': ignored changed file." );
            }
            return true;
        }

        bool IsActiveCultureFile( ReadOnlySpan<char> n )
        {
            foreach( var end in _activeNames )
            {
                if( n.EndsWith( end ) ) return true;
            }
            return false;
        }

        bool ILiveUpdater.ApplyChanges( IActivityMonitor monitor )
        {
            if( !_hasChanged ) return true;
            var f = _handler.GetUnambiguousFinalTranslations( monitor, _data );
            return f != null && _handler.WriteFinal( monitor, f, _installer );
        }

    }

}
