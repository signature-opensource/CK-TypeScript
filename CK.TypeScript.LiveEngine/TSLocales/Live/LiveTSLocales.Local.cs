using CK.Core;
using CK.EmbeddedResources;
using System.Collections.Generic;

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

        public bool ApplyLocaleCultureSet( IActivityMonitor monitor,
                                           IReadOnlySet<NormalizedCultureInfo> activeCultures,
                                           FinalLocaleCultureSet final )
        {
            if( !_p.Resources.LoadLocales( monitor, activeCultures, out var locales, "ts-locales" ) )
            {
                return false;
            }
            return locales == null || final.Add( monitor, locales );
        }
    }
}

