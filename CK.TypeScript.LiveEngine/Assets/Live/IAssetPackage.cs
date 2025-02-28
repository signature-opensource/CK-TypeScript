using CK.Core;
using CK.EmbeddedResources;

namespace CK.TypeScript.LiveEngine;

interface IAssetPackage
{
    bool ApplyResourceAssetSet( IActivityMonitor monitor, FinalResourceAssetSet final );
}
