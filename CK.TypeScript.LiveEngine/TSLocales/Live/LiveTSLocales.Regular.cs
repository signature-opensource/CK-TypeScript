using CK.Core;
using System.Collections.Generic;

namespace CK.TypeScript.LiveEngine;

sealed partial class LiveTSLocales
{
    /// <summary>
    /// Corresponds to one or more consecutive regular pakages.
    /// Only their compacted set matters. No need to have the
    /// details of each regular packages.
    /// </summary>
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

