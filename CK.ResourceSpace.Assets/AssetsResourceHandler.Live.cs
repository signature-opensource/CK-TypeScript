using CK.BinarySerialization;

namespace CK.Core;

public partial class AssetsResourceHandler : ILiveResourceSpaceHandler, ILiveUpdater
{
    bool ILiveResourceSpaceHandler.WriteLiveState( IActivityMonitor monitor, IBinarySerializer s, string ckWatchFolderPath )
    {
        s.Writer.Write( RootFolderName );
        return true;
    }

    public static ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResourceSpaceData data, IBinaryDeserializer d )
    {
        var rootFolderName = d.Reader.ReadString();
        return new AssetsResourceHandler( data.ResPackageDataCache, rootFolderName );
    }

    public bool OnChange( IActivityMonitor monitor, IResPackageResources resources, string filePath )
    {
        throw new System.NotImplementedException();
    }

    public bool ApplyChanges( IActivityMonitor monitor, IResourceSpaceFileInstaller installer )
    {
        throw new System.NotImplementedException();
    }
}
