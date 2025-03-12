using CK.Core;
using CK.EmbeddedResources;
using CK.Setup;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;

namespace CK.TypeScript.Engine;

sealed partial class TSLocalesResourceHandler : ResourceSpaceFolderHandler
{
    readonly IReadOnlySet<NormalizedCultureInfo> _activeCultures;
    LocaleCultureSet?[] _locales;
    LocaleCultureSet?[] _combinedLocales;
    // The final combined locales.
    // Finalized by a call to FinalLocaleCultureSet.PropagateFallbackTranslations.
    LocaleCultureSet? _buildFinal;
    // The live state is as simple as that (see BuildLiveState).
    LocaleCultureSet?[]? _liveState;

    public TSLocalesResourceHandler( ResourceSpaceData spaceData,
                                     IReadOnlySet<NormalizedCultureInfo> activeCultures,
                                     string folderName )
        : base( spaceData, folderName )
    {
        _locales = new LocaleCultureSet[spaceData.Packages.Length];
        _combinedLocales = new LocaleCultureSet[spaceData.Packages.Length];
        _activeCultures = activeCultures;
    }

    /// <summary>
    /// Gets the final locales set.
    /// Null if an error occurred during initialization.
    /// </summary>
    public LocaleCultureSet? BuildFinal => _buildFinal;

    /// <summary>
    /// Gets the live state.
    /// Null if no local packages exist or if an error occurred during initialization.
    /// </summary>
    public LocaleCultureSet?[]? LiveState => _liveState;

    protected override bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData )
    {
        // Consider all the packages (from "<Code>" to "<App>" included) that
        // have a "RootFolderName" (like "locales").
        // Combination is done with as final sets (isPartialSet: false): a override
        // that overrides nothing is an error.
        bool success = true;
        foreach( var p in spaceData.Packages )
        {
            success &= p.Resources.LoadLocales( monitor,
                                                _activeCultures,
                                                out var tsLocales,
                                                RootFolderName,
                                                isOverrideFolder: p == spaceData.AppPackage );
            // Combines them with all the ones from the reachable packages.
            LocaleCultureSet? combined = tsLocales;
            if( p.ReachablePackages.Count > 0 )
            {
                var combiner = new FinalLocaleCultureSet( isPartialSet: false, p.FullName );
                if( tsLocales != null )
                {
                    success &= combiner.Add( monitor, tsLocales );
                }
                foreach( var pAbove in p.ReachablePackages )
                {
                    var above = _combinedLocales[pAbove.Index];
                    if( above != null )
                    {
                        success &= combiner.Add( monitor, above );
                    }
                }
                combined = combiner.Root;
            }
            // We can have no locales for this package, but we may have a combination
            // from its requirements.
            _locales[p.Index] = tsLocales;
            _combinedLocales[p.Index] = combined;
        }
        // Now combines all the RootPackages combined locales and applies
        // the appLocales if any.
        if( success )
        {
            var finalCombiner = new FinalLocaleCultureSet( isPartialSet: false, "Final TSLocales" );
            foreach( var p in spaceData.RootPackages )
            {
                var l = _combinedLocales[p.Index];
                if( l != null )
                {
                    success &= finalCombiner.Add( monitor, l );
                }
            }
            if( success && appLocales != null )
            {
                success &= finalCombiner.Add( monitor, appLocales );
            }
            if( success )
            {
                finalCombiner.PropagateFallbackTranslations( monitor );
                _buildFinal = finalCombiner.Root;
                // Only on success do we build the live state.
                if( spaceData.LocalPackages.Length > 0 )
                {
                    _liveState = BuildLiveState( spaceData );
                }
            }
        }
        return success;
    }

    LocaleCultureSet?[] BuildLiveState( ResourceSpaceData spaceData )
    {
        // To support the live system in the best possible way, the LiveState
        // considers for each local package its minimal set of reachable packages.
        //  - If the required package is a local one, nothing is stored.
        //  - If the required package is not a local one and has no local package in
        //    its dependencies, we store its combined locales.
        //  - If the required package is not a local one but has at least one local package
        //    in its dependencies, we store its own locales and consider its dependencies
        //    in the same way.
        //
        // To handle the "ck-gen-transform/ts-locales", that is essentially a local package,
        // we also inject the root packages: the "final application" depends on all the
        // packages, including these roots that noone references.
        //
        LocaleCultureSet?[] liveLocales = new LocaleCultureSet[spaceData.Packages.Length];
        // The packages we eventually need to consider are the Local and all the interesting ones.
        // The ts-locales live state is independent from the core state (with the serialized ResSpace)
        // because... it can. This enables the Live to live without the ts-locales data until a
        // file in a "ts-locales/" folder changes.
        // To achieve this, serialization of the IResourceContainer is skipped. The ResFolder
        // and ResLocator use EmptyResourceContainer objects (with the original display name).
        // To handle OnChange, the LiveTSLocales relies on the ResSpace and its ResPackages, we don't
        // need to store here any package related information: the ResPackage.Index is enough
        // to work with the liveLocales array.
        var interesting = spaceData.LocalPackages.SelectMany( p => p.ReachablePackages )
                                                 .Concat( spaceData.RootPackages )
                                                 .Where( d => !d.IsLocalPackage )
                                                 .Distinct();
        foreach( var p in interesting )
        {
            if( !p.AllReachableHasLocalPackage )
            {
                // This non local package is " terminal" one: we keep its combined
                // locales as they are definitively computed.
                liveLocales[p.Index] = _combinedLocales[p.Index];
            }
            else
            {
                // This non local package has a dependency on at least one local.
                // We keep its resources locale to avoid reading them again from
                // the assembly.
                // The magic is that we don't need more: the local package(s) on
                // which it depends necessarily belongs to spaceData.LocalPackages,
                // their dependencies are handled
                liveLocales[p.Index] = _locales[p.Index];
            }
        }
        return liveLocales;
    }

}
