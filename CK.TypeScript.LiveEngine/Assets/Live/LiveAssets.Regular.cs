using CK.Core;
using CK.EmbeddedResources;

namespace CK.TypeScript.LiveEngine;

sealed partial class LiveAssets
{
    public sealed class Regular : IAssetPackage
    {
        readonly ResourceAssetDefinitionSet _assets;

        public Regular( ResourceAssetDefinitionSet assets )
        {
            _assets = assets;
        }

        public bool ApplyResourceAssetSet( IActivityMonitor monitor, FinalResourceAssetSet final )
        {
            return final.Add( monitor, _assets );
        }
    }

}
