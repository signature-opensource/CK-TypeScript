using CK.Core;
using System.Collections.Generic;

namespace CK.TypeScript.LiveEngine;

sealed class TSLocalesBuilder
{
    FinalLocaleCultureSet? _currentRegularLocales;
    int _regularPackageLocalesCount;
    // A LocalPackageRef or a LocaleCultureSet of
    // combined resources for one or more regular packages.
    List<object> _packageLocales;

    public TSLocalesBuilder()
    {
        _packageLocales = new List<object>();
    }

    public void AddRegularPackage( IActivityMonitor monitor, LocaleCultureSet locales )
    {
        Throw.DebugAssert( locales != null );
        bool isPartial = _regularPackageLocalesCount > 0;
        _currentRegularLocales ??= new FinalLocaleCultureSet( isPartial, isPartial ? $"Set n°{_regularPackageLocalesCount}": "Initial set" );
        _regularPackageLocalesCount++;
        _currentRegularLocales.Add( monitor, locales );
    }

    public void AddLocalPackage( IActivityMonitor monitor, LocalPackageRef localPackage )
    {
        if( _currentRegularLocales != null )
        {
            _packageLocales.Add( _currentRegularLocales );
            _currentRegularLocales = null;
        }
        _packageLocales.Add( localPackage );
    }

    public bool WriteTSLocalesState( IActivityMonitor monitor, string stateFolderPath )
    {
        return StateSerializer.WriteFile( monitor,
                                            stateFolderPath + TSLocaleSerializer.FileName,
                                            ( monitor, w ) => TSLocaleSerializer.WriteTSLocalesState( w, _packageLocales ) );
    }
}
