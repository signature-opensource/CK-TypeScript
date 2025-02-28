using CK.Core;
using CK.EmbeddedResources;
using System.Collections.Generic;

namespace CK.TypeScript.LiveEngine;

interface ITSLocalePackage
{
    bool ApplyLocaleCultureSet( IActivityMonitor monitor, IReadOnlySet<NormalizedCultureInfo> activeCultures, FinalLocaleCultureSet final );
}

