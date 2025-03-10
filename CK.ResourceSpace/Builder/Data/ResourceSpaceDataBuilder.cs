using CK.Setup;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Handles a <see cref="ResourceSpaceCollector"/> to topologically sort its configured <see cref="ResPackageDescriptor"/>
/// to produce a <see cref="ResourceSpaceData"/> with its final <see cref="ResPackage"/>.
/// </summary>
public sealed class ResourceSpaceDataBuilder
{
    readonly IReadOnlyDictionary<object, ResPackageDescriptor> _packageIndex;
    readonly IReadOnlyCollection<ResPackageDescriptor> _packages;
    readonly int _localPackageCount;

    public ResourceSpaceDataBuilder( ResourceSpaceCollector collector )
    {
        _packageIndex = collector.PackageIndex;
        _packages = collector.Packages;
        _localPackageCount = collector.LocalPackageCount;
    }

    /// <summary>
    /// Produces the <see cref="ResourceSpaceData"/> with its final <see cref="ResPackage"/>
    /// toplogically sorted. 
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>The space data on success, null otherwise.</returns>
    public ResourceSpaceData? Build( IActivityMonitor monitor )
    {
        var sortResult = DependencySorter<ResPackageDescriptor>.OrderItems( monitor,
                                                                            _packages,
                                                                            discoverers: null );
        if( !sortResult.IsComplete )
        {
            sortResult.LogError( monitor );
            return null;
        }
        // We can compute the final size of the index: it is the same as the builder index
        // with FullName, PackageResources and Type(?) plus the CodeGenResources instance.
        Throw.DebugAssert( sortResult.SortedItems != null );
        Throw.DebugAssert( "No items, only containers (and maybe groups).",
                            sortResult.SortedItems.All( s => s.IsGroup || s.IsGroupHead ) );

        var b = ImmutableArray.CreateBuilder<ResPackage>( _packages.Count );
        var bLocal = ImmutableArray.CreateBuilder<ResPackage>( _localPackageCount );
        var bRoot = ImmutableArray.CreateBuilder<ResPackage>();
        var packageIndex = new Dictionary<object, ResPackage>( _packageIndex.Count + _packages.Count );
        var space = new ResourceSpaceData( packageIndex );
        foreach( var s in sortResult.SortedItems )
        {
            if( s.IsGroup )
            {
                ResPackageDescriptor d = s.Item;
                // Close the CodeGen resources.
                d.CodeGenResources.Close();
                Throw.DebugAssert( "A child cannot be required and a requirement cannot be a child.",
                                   !s.Requires.Intersect( s.Children ).Any() );
                // Requirements and children have already been indexed.
                ImmutableArray<ResPackage> requires = s.Requires.Select( s => packageIndex[s.Item.CodeGenResources] ).ToImmutableArray();
                ImmutableArray<ResPackage> children = s.Children.Select( s => packageIndex[s.Item.CodeGenResources] ).ToImmutableArray();
                var p = new ResPackage( d.FullName,
                                        d.DefaultTargetPath,
                                        d.PackageResources,
                                        d.CodeGenResources,
                                        d.LocalPath,
                                        d.IsGroup,
                                        d.Type,
                                        requires,
                                        children,
                                        b.Count );
                // The 4 indexes.
                packageIndex.Add( p.FullName, p );
                packageIndex.Add( p.PackageResources, p );
                packageIndex.Add( p.CodeGenResources, p );
                if( p.Type != null )
                {
                    packageIndex.Add( p.Type, p );
                }
                // Rank is 1-based. Rank = 1 is for the head.
                if( s.Rank == 2 )
                {
                    bRoot.Add( p );
                }
                if( p.IsLocalPackage )
                {
                    bLocal.Add( p );
                }
            }
        }
        Throw.DebugAssert( "Expected planned size.", packageIndex.Count == _packageIndex.Count + _packages.Count );
        space._packages = b.MoveToImmutable();
        space._localPackages = bLocal.MoveToImmutable();
        // Number of roots cannot be computed.
        space._rootPackages = bRoot.DrainToImmutable();
        return space;
    }
}
