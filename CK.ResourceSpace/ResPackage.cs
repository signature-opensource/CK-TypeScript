using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

public sealed partial class ResPackage
{
    readonly ResSpaceData _spaceData;
    readonly string _fullName;
    readonly Type? _type;
    readonly NormalizedPath _defaultTargetPath;
    readonly ImmutableArray<ResPackage> _requires;
    readonly AggregateId _requiresAggregateId;
    readonly ImmutableArray<ResPackage> _children;
    readonly AggregateId _childrenAggregateId;
    readonly IReadOnlySet<ResPackage> _reachables;
    readonly IReadOnlySet<ResPackage> _afterReachables;

    readonly BeforeRes _resources;
    readonly AfterRes _afterResources;
    readonly int _index;
    readonly bool _isGroup;

    internal ResPackage( SpaceDataCacheBuilder dataCacheBuilder,
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
        Throw.DebugAssert( "The <Code> package is the first one.", (index == 0) == (fullName == "<Code>") );
        Throw.DebugAssert( "The <App> package is the last one.", (index == dataCacheBuilder.TotalPackageCount - 1) == (fullName == "<App>") );
        _fullName = fullName;
        _defaultTargetPath = defaultTargetPath;
        _isGroup = isGroup;
        _index = index;
        _requires = requires;
        _children = children;
        _type = type;
        _spaceData = dataCacheBuilder.SpaceData;
        // Initializes the resources.
        _resources = new BeforeRes( this, beforeResources, idxBeforeResources );
        _afterResources = new AfterRes( this, afterResources, idxAfterResources );

        if( _requires.Length == 0 )
        {
            _reachables = ImmutableHashSet<ResPackage>.Empty;
            Throw.DebugAssert( _requiresAggregateId == default );
        }
        else
        {
            _reachables = dataCacheBuilder.GetReachableClosure( _requires, out _requiresAggregateId );
            Throw.DebugAssert( _requiresAggregateId != default
                               && _requiresAggregateId.HasLocal == _reachables.Any( r => r.IsEventuallyLocalDependent ) );
        }
        Throw.DebugAssert( "Reachables and Children don't overlap.", !_reachables.Overlaps( children ) );

        if( children.Length == 0 )
        {
            _afterReachables = _reachables;
            Throw.DebugAssert( _childrenAggregateId == default );
        }
        else
        {
            _childrenAggregateId = dataCacheBuilder.RegisterAggregate( _children );
            var closure = new HashSet<ResPackage>( _reachables );
            foreach( var c in _children )
            {
                if( closure.Add( c ) )
                {
                    closure.UnionWith( c._afterReachables );
                }
            }
            _afterReachables = closure;
            Throw.DebugAssert( _childrenAggregateId != default
                              && _childrenAggregateId.HasLocal == _children.SelectMany( c => c.AfterReachables ).Concat( _children )
                                                                           .Any( r => r.IsEventuallyLocalDependent ) );
        }
        Throw.DebugAssert( _afterReachables == _reachables || _afterReachables.IsProperSupersetOf( _reachables ) );
    }

    internal (AggregateId RequiresAggregateId, AggregateId ChildrenAggregateId) GetAggregateIdentifiers() => (_requiresAggregateId, _childrenAggregateId);

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
    public IResPackageResources AfterResources => _afterResources;

    /// <summary>
    /// Gets whether this is a locally available package.
    /// </summary>
    public bool IsLocalPackage => _resources.LocalPath != null || _afterResources.LocalPath != null;

    /// <summary>
    /// Gets the type if this package is defined by a type.
    /// </summary>
    public Type? Type => _type;

    /// <summary>
    /// Gets whether this package is a group.
    /// </summary>
    public bool IsGroup => _isGroup;

    /// <summary>
    /// Gets the index in the <see cref="ResSpaceData.Packages"/>.
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
    /// Gets the set of packages that this package contains.
    /// <para>
    /// A child package is not a requirement and a requirement cannot be a child.
    /// </para>
    /// </summary>
    public ImmutableArray<ResPackage> Children => _children;

    /// <summary>
    /// Gets the packages that are reachable from this one: this is
    /// the closure of this <see cref="Requires"/> and their <see cref="Reachables"/>.
    /// <para>
    /// This excludes the children of this package, this is the point of view of the "head"
    /// of the package.
    /// </para>
    /// <para>
    /// The <see cref="AfterReachables"/> is the same set but from
    /// the point of view of the "tail" of the package that includes the <see cref="Reachables"/>
    /// of the <see cref="Children"/>.
    /// </para>
    /// </summary>
    public IReadOnlySet<ResPackage> Reachables => _reachables;

    /// <summary>
    /// Gets the packages that are reachable from the "tail" of this package:
    /// this extends <see cref="Reachables"/> with the <see cref="Reachables"/>
    /// of the <see cref="Children"/>.
    /// </summary>
    public IReadOnlySet<ResPackage> AfterReachables => _afterReachables;

    /// <summary>
    /// Gets whether this <see cref="IsLocalPackage"/> is true or one of the <see cref="AfterReachables"/>
    /// is local.
    /// </summary>
    public bool IsEventuallyLocalDependent => _requiresAggregateId.HasLocal || _childrenAggregateId.HasLocal || IsLocalPackage;

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
