using CK.Core;
using CK.EmbeddedResources;

namespace CK.TypeScript.LiveEngine;

sealed partial class LiveAssets
{
    public sealed class Regular : IAssetPackage
    {
        readonly ResourceAssetSet _assets;

        public Regular( ResourceAssetSet assets )
        {
            _assets = assets;
        }

        public bool ApplyResourceAssetSet( IActivityMonitor monitor, FinalResourceAssetSet final )
        {
            return final.Add( monitor, _assets );
        }
    }

}
