using CK.Core;
using System.Collections.Generic;

namespace CK.TypeScript.LiveEngine;

sealed partial class LiveTSLocales
{
    public sealed class Regular : ITSLocalePackage
    {
        readonly LocaleCultureSet _tsLocales;

        public Regular( LocaleCultureSet tsLocales )
        {
            _tsLocales = tsLocales;
        }

        public bool ApplyLocaleCultureSet( IActivityMonitor monitor, IReadOnlySet<NormalizedCultureInfo> activeCultures, FinalLocaleCultureSet final )
        {
            return final.Add( monitor, _tsLocales );
        }
    }
}

