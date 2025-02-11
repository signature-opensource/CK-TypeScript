using CK.Core;

namespace CK.TypeScript.LiveEngine;

interface ITSLocalePackage
{
    bool ApplyLocaleCultureSet( IActivityMonitor monitor, LiveState state, FinalLocaleCultureSet final );
}

