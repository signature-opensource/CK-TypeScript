using CK.Core;

namespace CK.TypeScript.Engine;

public sealed class TSAssetsResourceHandler : AssetsResourceHandler
{
    public TSAssetsResourceHandler( ResourceSpaceData spaceData )
        : base( spaceData.ResPackageDataCache, "ts-assets" )
    {
    }

    public bool Initialize( IActivityMonitor monitor, ResourceSpaceBuilder spaceBuilder )
    {
        Throw.CheckState( spaceBuilder.SpaceData.ResPackageDataCache == ResPackageDataCache );
        return spaceBuilder.RegisterHandler( monitor, this );
    }


}
