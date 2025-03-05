using CK.Core;
using CK.EmbeddedResources;
using System.Collections.Generic;

namespace CK.TypeScript.Engine;

sealed partial class TSLocalesResourceHandler
{
    /// <summary>
    /// Encapsulates the Live state creation.
    /// For performance reasons, this works in topological degraded mode (only package ordering
    /// is considered for conflicts and overrides).
    /// </summary>
    struct LiveStateBuilder
    {
        readonly TSLocalesResourceHandler _handler;
        FinalLocaleCultureSet? _currentRegularLocales;
        int _regularPackageLocalesCount;
        // A ResPackage for local packages or a LocaleCultureSet of combined
        // resources of one or more regular packages.
        List<object> _packageLocales;

        public LiveStateBuilder( TSLocalesResourceHandler handler )
        {
            _packageLocales = new List<object>();
            _handler = handler;
        }

        public void AddRegularPackage( IActivityMonitor monitor, LocaleCultureSet locales )
        {
            Throw.DebugAssert( locales != null );
            bool isPartial = _regularPackageLocalesCount > 0;
            _currentRegularLocales ??= new FinalLocaleCultureSet( isPartial, isPartial ? $"Set n°{_regularPackageLocalesCount}" : "Initial set" );
            _regularPackageLocalesCount++;
            _currentRegularLocales.Add( monitor, locales );
        }

        public void AddLocalPackage( IActivityMonitor monitor, ResPackage localPackage )
        {
            Throw.DebugAssert( localPackage.LocalPath != null );
            if( _currentRegularLocales != null )
            {
                _packageLocales.Add( _currentRegularLocales.Root );
                _currentRegularLocales = null;
            }
            _packageLocales.Add( localPackage );
        }
    }
}
