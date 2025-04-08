using CK.BinarySerialization;

namespace CK.Core;

public partial class AssetsResourceHandler : ILiveResourceSpaceHandler, ILiveUpdater
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
        s.Writer.Write( RootFolderName );
        return true;
    }

    public static ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResourceSpaceData data, IBinaryDeserializer d )
    {
        var installer = new FileSystemInstaller( d.Reader.ReadString() );
        var rootFolderName = d.Reader.ReadString();
        return new AssetsResourceHandler( installer, data.ResPackageDataCache, rootFolderName );
    }

    public bool OnChange( IActivityMonitor monitor, IResPackageResources resources, string filePath )
    {
        throw new System.NotImplementedException();
    }

    public bool ApplyChanges( IActivityMonitor monitor )
    {
        throw new System.NotImplementedException();
    }
}
