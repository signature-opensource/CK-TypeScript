using CK.BinarySerialization;
using System;
using System.Linq;

namespace CK.Core;

public partial class LocalesResourceHandler : ILiveResourceSpaceHandler, ILiveUpdater
{
    bool ILiveResourceSpaceHandler.WriteLiveState( IActivityMonitor monitor, IBinarySerializer s, string ckWatchFolderPath )
    {
        s.Writer.Write( string.Join( ',', _cache.ActiveCultures.AllActiveCultures.Select( c => c.Culture.Name ) ) );
        s.Writer.Write( RootFolderName );
        s.Writer.WriteNonNegativeSmallInt32( (int)_installOption );
        return true;
    }

    public static ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResourceSpaceData data, IBinaryDeserializer d )
    {
        var cultures = d.Reader.ReadString().Split( ',' ).Select( NormalizedCultureInfo.EnsureNormalizedCultureInfo );
        var activeCultures = new ActiveCultureSet( cultures );
        var rootFolderName = d.Reader.ReadString();
        var options = (InstallOption)d.Reader.ReadNonNegativeSmallInt32();
        return new LocalesResourceHandler( data.ResPackageDataCache, rootFolderName, activeCultures, options );
    }

    bool ILiveUpdater.OnChange( IActivityMonitor monitor, IResPackageResources resources, string filePath )
    {
        throw new NotImplementedException();
    }

    bool ILiveUpdater.ApplyChanges( IActivityMonitor monitor, IResourceSpaceFileInstaller installer )
    {
        throw new NotImplementedException();
    }
}
