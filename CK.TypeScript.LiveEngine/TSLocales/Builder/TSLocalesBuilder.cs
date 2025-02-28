using CK.Core;
using CK.EmbeddedResources;
using System.Collections.Generic;

namespace CK.TypeScript.LiveEngine;

/// <summary>
/// TSLocales are independent of the LiveState because the regular packages resources
/// don't need to be extracted at runtime since the LocaleCultureSet capture the
/// all the resources needed once for all. The state is stored in an independent
/// file <see cref="TSLocaleSerializer.FileName"/>.
/// <para>
/// This compacts multiple consecutive regular packages ts-locales into one <see cref="FinalLocaleCultureSet"/>,
/// so that we don"t need to know the real RegularPackage type.
/// </para>
/// </summary>
sealed class TSLocalesBuilder
{
    FinalLocaleCultureSet? _currentRegularLocales;
    int _regularPackageLocalesCount;
    // A LocalPackageRef or a LocaleCultureSet of combined
    // resources of one or more regular packages.
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
            _packageLocales.Add( _currentRegularLocales.Root );
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
