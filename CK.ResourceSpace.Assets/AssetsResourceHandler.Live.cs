using CK.BinarySerialization;
using System.Reflection.Metadata;
using System;

namespace CK.Core;

public partial class AssetsResourceHandler : ILiveResourceSpaceHandler
{
    /// <summary>
    /// Live update is currently supported only when installing on the file system:
    /// the <see cref="FileSystemInstaller.TargetPath"/> is serialized in the live state
    /// and a basic <see cref="FileSystemInstaller"/> is deserialized to be the installer
    /// on the live side.
    /// </summary>
    public bool DisableLiveUpdate => Installer is not FileSystemInstaller;

    bool ILiveResourceSpaceHandler.WriteLiveState( IActivityMonitor monitor, IBinarySerializer s, ResSpaceData spaceData )
    {
        Throw.DebugAssert( "Otherwise LiveState would have been disabled.", Installer is FileSystemInstaller );
        s.Writer.Write( ((FileSystemInstaller)Installer).TargetPath );
        s.Writer.Write( RootFolderName );
        return true;
    }

    public static ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResSpaceData data, IBinaryDeserializer d )
    {
        var installer = new FileSystemInstaller( d.Reader.ReadString() );
        var rootFolderName = d.Reader.ReadString();
        var handler = new AssetsResourceHandler( installer, data.SpaceDataCache, rootFolderName );
        return new LiveUpdater( handler, installer, data );
    }


    sealed class LiveUpdater : ILiveUpdater
    {
        readonly AssetsResourceHandler _handler;
        readonly FileSystemInstaller _installer;
        readonly ResSpaceData _data;
        bool _hasChanged;

        public LiveUpdater( AssetsResourceHandler handler, FileSystemInstaller installer, ResSpaceData data )
        {
            _handler = handler;
            _installer = installer;
            _data = data;
        }

        public bool OnChange( IActivityMonitor monitor, IResPackageResources resources, string filePath )
        {
            if( !IsFileInRootFolder( _handler.RootFolderName, filePath, out ReadOnlySpan<char> localFile ) )
            {
                return false;
            }
            _hasChanged = true;
            _handler._cache.InvalidateCache( monitor, resources );
            return true;
        }

        public bool ApplyChanges( IActivityMonitor monitor )
        {
            if( !_hasChanged ) return true;
            _hasChanged = false;
            var f = _handler.GetUnambiguousFinalAssets( monitor, _data );
            return f != null && WriteFinal( monitor, f, _installer );
        }
    }
}
