using CK.Core;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace CK.TypeScript.LiveEngine;

sealed partial class LiveTSLocales
{
    readonly LiveState _state;
    ImmutableArray<ITSLocalePackage> _packages;

    public LiveTSLocales( LiveState state )
    {
        _state = state;
    }

    public bool IsActive => !_packages.IsDefault;

    public bool Load( IActivityMonitor monitor )
    {
        var a = StateSerializer.ReadFile( monitor,
                                              _state.LoadFolder.AppendPart( TSLocaleSerializer.FileName ),
                                              ( monitor, r ) => TSLocaleSerializer.ReadLiveTSLocales( monitor, r, _state.LocalPackages ) );
        if( a == null )
        {
            _packages = default;
            return false;
        }
        _packages = ImmutableCollectionsMarshal.AsImmutableArray( a );
        return true;
    }
}

