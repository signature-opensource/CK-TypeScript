using CK.BinarySerialization;
using System;

namespace CK.Core;

public partial class LocalesResourceHandler : ILiveResourceSpaceHandler
{
    public bool WriteLiveState( IActivityMonitor monitor, IBinarySerializer s, string ckWatchFolderPath )
    {
        throw new NotImplementedException();
    }

    public static ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResourceSpaceData data, IBinaryDeserializer d )
    {
        throw new NotImplementedException();
    }
}
