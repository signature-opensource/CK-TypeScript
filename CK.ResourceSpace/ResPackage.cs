using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CK.Core;

public sealed partial class ResPackage
{
    readonly string _fullName;
    readonly Type? _type;
    readonly NormalizedPath _defaultTargetPath;
    readonly ImmutableArray<ResPackage> _requires;
    readonly IReadOnlySet<ResPackage> _reachablePackages;
    readonly AggregateId _reachableAggregateId;
    readonly ImmutableArray<ResPackage> _children;
    readonly AggregateId _childrenAggregateId;
    readonly IReadOnlySet<ResPackage> _allReachablePackages;
    readonly BeforeRes _resources;
    readonly AfterRes _resourcesAfter;
    readonly int _index;
    readonly bool _isGroup;
    readonly bool _requiresHasLocalPackage;
    readonly bool _reachableHasLocalPackage;
    readonly bool _allReachableHasLocalPackage;
    // Content information.
    readonly IReadOnlySet<ResPackage> _afterReachablePackages;
    readonly IReadOnlySet<ResPackage> _allAfterReachablePackages;
    readonly bool _childrenHasLocalPackage;
    readonly bool _afterReachableHasLocalPackage;
    readonly bool _allAfterReachableHasLocalPackage;
    // Implementation notes:
    // ResPackage doesn't reference the ResourceSpaceData.
    // To be able to locate the package of a resource (to check reachability of resources)
    // we however need a resourceIndex.
    // This resource index is shared by all ResPackage and is the only "global" context
    // we really need.
    readonly IReadOnlyDictionary<IResourceContainer, IResPackageResources> _resourceIndex;

    internal ResPackage( ResPackageDataCacheBuilder dataCacheBuilder,
                         string fullName,
                         NormalizedPath defaultTargetPath,
                         int idxBeforeResources,
                         IResourceContainer beforeResources,
                         int idxAfterResources,
                         IResourceContainer afterResources,
                         bool isGroup,
                         Type? type,
                         ImmutableArray<ResPackage> requires,
                         ImmutableArray<ResPackage> children,
                         int index )
    {
        _fullName = fullName;
        _defaultTargetPath = defaultTargetPath;
        _isGroup = isGroup;
        _index = index;
        _requires = requires;
        _children = children;
        _type = type;
        _resourceIndex = dataCacheBuilder._resourceIndex;
        // Initializes the resources.
        _resources = new BeforeRes( this, beforeResources, idxBeforeResources );
        _resourcesAfter = new AfterRes( this, afterResources, idxAfterResources );

        // Reacheable is the core set (deduplicated Requires + Requires' Children).
        bool allIsRequired;
        if( requires.Length > 0 )
        {
            var reachablePackages = new HashSet<ResPackage>();
            (_requiresHasLocalPackage, _reachableHasLocalPackage, allIsRequired) = ComputeReachablePackages( reachablePackages );
            _reachablePackages = dataCacheBuilder.RegisterAndShare( reachablePackages, out _reachableAggregateId );
        }
        else
        {
            allIsRequired = false;
            _reachablePackages = ImmutableHashSet<ResPackage>.Empty;
            Throw.DebugAssert( _reachableAggregateId == default );
        }
        // AllReacheable. ComputeReachablePackages above computed the allIsRequired.
        if( allIsRequired )
        {
            var allReachablePackages = new HashSet<ResPackage>();
            _allReachableHasLocalPackage = ComputeAllReachablePackages( allReachablePackages )
                                           || _reachableHasLocalPackage;
            Throw.DebugAssert( "allIsRequired should have been false!", allReachablePackages.Count > _reachablePackages.Count );
            _allReachablePackages = allReachablePackages;
        }
        else
        {
            _allReachablePackages = _reachablePackages;
            _allReachableHasLocalPackage = _reachableHasLocalPackage;
        }
        // Content:
        // AfterReachable is the ReachablePackages + Children.
        // AllAfterReacheable is the AllReachable + Children's AllAfterReachable.
        // For both of them, if we have no children, they are the Reachable (resp. AllReachable)
        // and _childrenHasLocalPackage obviously remains false.
        Throw.DebugAssert( "ReachablePackages and Children don't overlap.",
                           !_reachablePackages.Overlaps( children ) );
        if( children.Length == 0 )
        {
            _afterReachablePackages = _reachablePackages;
            _afterReachableHasLocalPackage = _reachableHasLocalPackage;
            _allAfterReachablePackages = _allReachablePackages;
            _allAfterReachableHasLocalPackage = _allReachableHasLocalPackage;
            Throw.DebugAssert( _childrenAggregateId == default );
        }
        else
        {
            _childrenAggregateId = dataCacheBuilder.RegisterAggregate( _children );
            // ComputeAllContentReachablePackage computes the _childrenHasLocalPackage, we compute it first.
            // It contains the children (just like the _afterReachablePackages computed below).
            var allAfterReachablePackages = new HashSet<ResPackage>( _allReachablePackages );
            (_childrenHasLocalPackage, _allAfterReachableHasLocalPackage) = ComputeAllContentReachablePackage( allAfterReachablePackages );
            _allAfterReachableHasLocalPackage |= _allReachableHasLocalPackage;
            _allAfterReachablePackages = allAfterReachablePackages;

            _afterReachableHasLocalPackage = _reachableHasLocalPackage || _childrenHasLocalPackage;
            var afterReachablePackages = new HashSet<ResPackage>( _reachablePackages.Count + children.Length );
            afterReachablePackages.AddRange( _reachablePackages );
            afterReachablePackages.AddRange( _children );
            _afterReachablePackages = afterReachablePackages;
        }
    }

    (bool,bool,bool) ComputeReachablePackages( HashSet<ResPackage> set )
    {
        Throw.DebugAssert( "Initial set must be empty.", set.Count == 0 );
        bool allRequired = false;
        bool rL = false;
        bool l = false;
        foreach( var p in _requires )
        {
            // Adds the requirements. A previously added requirement's child
            // may also appear in our requirements.
            if( set.Add( p ) )
            {
                if( p.IsLocalPackage )
                {
                    rL = l = true;
                }
                allRequired |= p._reachablePackages != p._allReachablePackages;
                // Consider the children of the requirement (and only them).
                foreach( var c in p._children )
                {
                    l |= p.IsLocalPackage;
                    allRequired |= p._reachablePackages != p._allReachablePackages;
                    set.Add( c );
                }
            }
        }
        return (rL,l,allRequired);
    }

    bool ComputeAllReachablePackages( HashSet<ResPackage> set )
    {
        // Don't start from the ReachablePackages. Simply aggregates
        // the requirement's AllReachablePackages.
        Throw.DebugAssert( "Initial set must be empty.", set.Count == 0 );
        bool l = false;
        foreach( var p in _requires )
        {
            if( set.Add( p ) )
            {
                l |= p._allReachableHasLocalPackage;
                set.UnionWith( p._allReachablePackages );
            }
        }
        return l;
    }

    (bool,bool) ComputeAllContentReachablePackage( HashSet<ResPackage> set )
    {
        // We extend our AllReachablePackages with our content's AllContentReachable.
        Throw.DebugAssert( "Initial set must be the AllReachablePackages.",
                           set.SetEquals( _allReachablePackages ) );
        bool cL = false;
        bool l = false;
        foreach( var p in _children )
        {
            Throw.DebugAssert( !set.Contains( p ) );
            set.Add( p );
            cL |= p.IsLocalPackage;
            l |= p.AllAfterReachableHasLocalPackage;
            set.UnionWith( p._allAfterReachablePackages );
        }
        return (cL,l);
    }

    internal (AggregateId, AggregateId) GetAggregateIdentifiers() => (_reachableAggregateId, _childrenAggregateId);

    /// <summary>
    /// Gets this package full name. When built from a type, this is the type's full name.
    /// </summary>
    public string FullName => _fullName;

    /// <summary>
    /// Gets the default target path that will prefix resources that are items.
    /// </summary>
    public NormalizedPath DefaultTargetPath => _defaultTargetPath;

    /// <summary>
    /// Gets the <see cref="IResPackageResources"/> for this package.
    /// </summary>
    public IResPackageResources Resources => _resources;

    /// <summary>
    /// Gets the <see cref="IResPackageResources"/> that apply after this package's <see cref="Children"/>
    /// and these <see cref="Resources"/>.
    /// </summary>
    public IResPackageResources ResourcesAfter => _resourcesAfter;

    /// <summary>
    /// Gets whether this is a locally available package.
    /// </summary>
    public bool IsLocalPackage => _resources.LocalPath != null || _resourcesAfter.LocalPath != null;

    /// <summary>
    /// Gets the type if this package is defined by a type.
    /// </summary>
    public Type? Type => _type;

    /// <summary>
    /// Gets whether this package is a group.
    /// </summary>
    public bool IsGroup => _isGroup;

    /// <summary>
    /// Gets the index in the <see cref="ResourceSpaceData.Packages"/>.
    /// This is 1-based (0 is an invalid index).
    /// </summary>
    public int Index => _index;

    /// <summary>
    /// Gets the direct set of packages that this package requires.
    /// <para>
    /// A child package is not a requirement and a requirement cannot be a child.
    /// </para>
    /// </summary>
    public ImmutableArray<ResPackage> Requires => _requires;

    /// <summary>
    /// Gets whether at least one of the <see cref="Requires"/> is a local package.
    /// </summary>
    public bool RequiresHasLocalPackage => _requiresHasLocalPackage;

    /// <summary>
    /// Gets the set of packages that this package contains.
    /// <para>
    /// A child package is not a requirement and a requirement cannot be a child.
    /// </para>
    /// </summary>
    public ImmutableArray<ResPackage> Children => _children;

    /// <summary>
    /// Gets whether at least one of the <see cref="Children"/> is a local package.
    /// </summary>
    public bool ChildrenHasLocalPackage => _childrenHasLocalPackage;

    /// <summary>
    /// Gets the packages that are reachable from this one: this is
    /// the <see cref="Requires"/> and the <see cref="Children"/>'s Requires
    /// (but not our Children). This set is minimal, it doesn't contain any
    /// transitive dependency.
    /// <para>
    /// This excludes the children of this package, this is the point of view of the "head"
    /// of the package.
    /// </para>
    /// <para>
    /// The <see cref="AfterReachablePackages"/> is the same minimal set but from
    /// the point of view of the "tail" of the package from which the children
    /// of the packages are like package's requirements.
    /// </para>
    /// </summary>
    public IReadOnlySet<ResPackage> ReachablePackages => _reachablePackages;

    /// <summary>
    /// Gets whether at least one of the <see cref="ReachablePackages"/> is a local package.
    /// </summary>
    public bool ReachableHasLocalPackage => _reachableHasLocalPackage;

    /// <summary>
    /// Gets all the packages that are reachable from this one: this is the transitive closure
    /// of the <see cref="ReachablePackages"/>.
    /// </summary>
    public IReadOnlySet<ResPackage> AllReachablePackages => _allReachablePackages;

    /// <summary>
    /// Gets whether at least one of the <see cref="AllReachablePackages"/> is a local package.
    /// </summary>
    public bool AllReachableHasLocalPackage => _allReachableHasLocalPackage;

    /// <summary>
    /// Gets the packages that are reachable from the 'tail" of this package: this is
    /// the <see cref="ReachablePackages"/> plus the <see cref="Children"/>.
    /// This set is minimal, it doesn't contain any transitive dependency and doesn't contain this package.
    /// </summary>
    public IReadOnlySet<ResPackage> AfterReachablePackages => _afterReachablePackages;

    /// <summary>
    /// Gets whether at least one of the <see cref="AfterReachablePackages"/> is a local package.
    /// </summary>
    public bool AfterReachableHasLocalPackage => _afterReachableHasLocalPackage;

    /// <summary>
    /// Gets all the packages that are reachable from the 'tail" of this package:
    /// this is the transitive closure of the <see cref="AfterReachablePackages"/> (but
    /// still without this package).
    /// </summary>
    public IReadOnlySet<ResPackage> AllAfterReachablePackages => _allAfterReachablePackages;

    /// <summary>
    /// Gets whether at least one of the <see cref="AllAfterReachablePackages"/> is a local package.
    /// </summary>
    public bool AllAfterReachableHasLocalPackage => _allAfterReachableHasLocalPackage;

    /// <summary>
    /// Gets whether this package is local dependent: either <see cref="AllAfterReachableHasLocalPackage"/>
    /// or <see cref="IsLocalPackage"/> is true.
    /// </summary>
    public bool IsEventuallyLocalDependent => _afterReachableHasLocalPackage || IsLocalPackage;

    /// <summary>
    /// Gets the <see cref="FullName"/> (type name if this package is defined by a type).
    /// </summary>
    /// <returns>The package full name.</returns>
    public override string ToString() => ToString( _fullName, _type );

    internal static string ToString( string fullName, Type? type )
    {
        return type != null && !fullName.EndsWith( type.Name )
                ? $"{fullName} ({type.Name})"
                : fullName;
    }
}
