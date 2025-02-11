using CK.Core;

namespace CK.TypeScript.LiveEngine;

sealed partial class LiveTSLocales
{
    public sealed class Local : ITSLocalePackage
    {
        readonly LocalPackage _p;

        public Local( LocalPackage p )
        {
            _p = p;
        }

        public bool ApplyLocaleCultureSet( IActivityMonitor monitor, LiveState state, FinalLocaleCultureSet final )
        {
            if( !_p.Resources.LoadLocales( monitor, state.ActiveCultures, out var locales ) )
            {
                return false;
            }
            return locales == null || final.Add( monitor, locales );
        }
    }
}

