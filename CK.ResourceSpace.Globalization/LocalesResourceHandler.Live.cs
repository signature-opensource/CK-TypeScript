using CK.BinarySerialization;
using System;
using System.Linq;

namespace CK.Core;

public partial class LocalesResourceHandler : ILiveResourceSpaceHandler, ILiveUpdater
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

    public static ILiveUpdater? ReadLiveState( IActivityMonitor monitor, ResourceSpaceData data, IBinaryDeserializer d )
    {
        var installer = new FileSystemInstaller( d.Reader.ReadString() );
        var cultures = d.Reader.ReadString().Split( ',' ).Select( NormalizedCultureInfo.EnsureNormalizedCultureInfo );
        var activeCultures = new ActiveCultureSet( cultures );
        var rootFolderName = d.Reader.ReadString();
        var options = (InstallOption)d.Reader.ReadNonNegativeSmallInt32();
        return new LocalesResourceHandler( installer, data.ResPackageDataCache, rootFolderName, activeCultures, options );
    }

    bool ILiveUpdater.OnChange( IActivityMonitor monitor, IResPackageResources resources, string filePath )
    {
        throw new NotImplementedException();
    }

    bool ILiveUpdater.ApplyChanges( IActivityMonitor monitor )
    {
        throw new NotImplementedException();
    }
}
