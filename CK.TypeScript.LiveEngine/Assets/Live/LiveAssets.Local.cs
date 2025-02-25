using CK.Core;

namespace CK.TypeScript.LiveEngine;

sealed partial class LiveAssets
{
    public sealed class Local : IAssetPackage
    {
        readonly LocalPackage _p;

        public Local( LocalPackage p )
        {
            _p = p;
        }

        public bool ApplyResourceAssetSet( IActivityMonitor monitor, FinalResourceAssetSet final )
        {
            if( !_p.Resources.LoadAssets( monitor, _p.TypeScriptFolder, out var locales ) )
            {
                return false;
            }
            return locales == null || final.Add( monitor, locales );
        }
    }

}
