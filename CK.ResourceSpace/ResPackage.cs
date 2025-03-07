using CK.EmbeddedResources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

namespace CK.Core;

public sealed partial class ResPackage
{
    readonly string _fullName;
    readonly Type? _type;
    readonly NormalizedPath _defaultTargetPath;
    readonly IResourceContainer _packageResources;
    readonly CodeGenResourceContainer _codeGenResources;
    readonly string? _localPath;
    readonly ImmutableArray<ResPackage> _requires;
    readonly ImmutableArray<ResPackage> _children;
    readonly HashSet<ResPackage> _allReachablePackages;
    readonly HashSet<ResPackage> _reachablePackages;
    // Content (less used) are created on demand.
    HashSet<ResPackage>? _contentReachablePackages;
    HashSet<ResPackage>? _allContentReachablePackages;
    readonly int _index;
    readonly bool _isGroup;
    readonly bool _requiresHasLocalPackage;
    readonly bool _reachableHasLocalPackage;
    readonly bool _allReachableHasLocalPackage;
    bool _childrenHasLocalPackage;
    bool _contentReachableHasLocalPackage;
    bool _allContentReachableHasLocalPackage;

    internal ResPackage( string fullName,
                         NormalizedPath defaultTargetPath,
                         IResourceContainer packageResources,
                         CodeGenResourceContainer codeGenResources,
                         string? localPath,
                         bool isGroup,
                         Type? type,
                         ImmutableArray<ResPackage> requires,
                         ImmutableArray<ResPackage> children,
                         int index )
    {
        _fullName = fullName;
        _defaultTargetPath = defaultTargetPath;
        _packageResources = packageResources;
        _codeGenResources = codeGenResources;
        _localPath = localPath;
        _isGroup = isGroup;
        _index = index;
        _requires = requires;
        _children = children;
        _type = type;
        // Reacheable is the core set (deduplicated Requires + Requires' Children).
        _reachablePackages = new HashSet<ResPackage>();
        (_requiresHasLocalPackage, _reachableHasLocalPackage, bool allRequired) = ComputeReachablePackages( _reachablePackages );

        // AllReacheable. ComputeReachablePackages above computed the allRequired.
        if( allRequired )
        {
            _allReachablePackages = new HashSet<ResPackage>();
            _allReachableHasLocalPackage = ComputeAllReachablePackages( _allReachablePackages )
                                           || _reachableHasLocalPackage;
            Throw.DebugAssert( "allRequired should have been false!", _allReachablePackages.Count > _reachablePackages.Count );
        }
        else
        {
            _allReachablePackages = _reachablePackages;
            _allReachableHasLocalPackage = _reachableHasLocalPackage;
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
        // We extend our AllReachablePackages with our content's AllContentReachableHasLocalPackage.
        Throw.DebugAssert( "Initial set must be the AllReachablePackages.",
                           set.SetEquals( _allReachablePackages ) );
        bool cL = false;
        bool l = false;
        foreach( var p in _children )
        {
            Throw.DebugAssert( !set.Contains( p ) );
            set.Add( p );
            cL |= p.IsLocalPackage;
            l |= p._allContentReachableHasLocalPackage;
            set.UnionWith( p._allContentReachablePackages );
        }
        return (cL,l);
    }

    [MemberNotNull( nameof( _contentReachablePackages ), nameof( _allContentReachablePackages ) )]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EnsureContent()
    {
        if( _contentReachablePackages == null ) InitializeContent();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void InitializeContent()
    {
        // ContentReachable is the ReachablePackages + Children.
        // AllContentReacheable is the AllReachable + Children's AllContentReachable.
        // For both of them, if we have no children, they are the Reachable (resp. AllReachable)
        // and _childrenHasLocalPackage obviously remains false.
        Throw.DebugAssert( "ReachablePackages and Children don't overlap.",
                           !_reachablePackages.Overlaps( _children ) );
        if( _children.Length == 0 )
        {
            _contentReachablePackages = _reachablePackages;
            _contentReachableHasLocalPackage = _reachableHasLocalPackage;
            _allContentReachablePackages = _allReachablePackages;
            _allContentReachableHasLocalPackage = _allReachableHasLocalPackage;
        }
        else
        {
            // AllContentReacheable computes the _childrenHasLocalPackage.
            _allContentReachablePackages = new HashSet<ResPackage>( _allReachablePackages );
            (_childrenHasLocalPackage, _allContentReachableHasLocalPackage) = ComputeAllContentReachablePackage( _allContentReachablePackages );
            _allContentReachableHasLocalPackage |= _allReachableHasLocalPackage;

            _contentReachablePackages = new HashSet<ResPackage>( _reachablePackages );
            _contentReachablePackages.AddRange( _children );
            _contentReachableHasLocalPackage = _reachableHasLocalPackage || _childrenHasLocalPackage;
        }
    }

    /// <summary>
    /// Gets this package full name. When built from a type, this is the type's full name.
    /// </summary>
    public string FullName => _fullName;

    /// <summary>
    /// Gets the default target path that will prefix resources that are items.
    /// </summary>
    public NormalizedPath DefaultTargetPath => _defaultTargetPath;

    /// <summary>
    /// Gets a closed set of resources that have been generated by code.
    /// </summary>
    public CodeGenResourceContainer CodeGenResources => _codeGenResources;

    /// <summary>
    /// Gets the resources of the package. <see cref="IResourceContainer.IsValid"/> is necessarily true
    /// but this can be a <see cref="EmptyResourceContainer"/>.
    /// </summary>
    public IResourceContainer PackageResources => _packageResources;

    /// <summary>
    /// Gets a non null fully qualified path of this package's resources if this is a local package.
    /// </summary>
    public string? LocalPath => _localPath;

    /// <summary>
    /// Gets whether this is a locally available package.
    /// </summary>
    [MemberNotNullWhen(true,nameof(LocalPath))]
    public bool IsLocalPackage => _localPath != null;

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
    public bool ChildrenHasLocalPackage
    {
        get
        {
            EnsureContent();
            return _childrenHasLocalPackage;
        }
    }

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
    /// The <see cref="ContentReachablePackages"/> is the same minimal set but from
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
    /// This set is minimal, it doesn't contain any transitive dependency.
    /// </summary>
    public IReadOnlySet<ResPackage> ContentReachablePackages
    {
        get
        {
            EnsureContent();
            return _contentReachablePackages;
        }
    }

    /// <summary>
    /// Gets whether at least one of the <see cref="ContentReachablePackages"/> is a local package.
    /// </summary>
    public bool ContentReachableHasLocalPackage
    {
        get
        {
            EnsureContent();
            return _contentReachableHasLocalPackage;
        }
    }

    /// <summary>
    /// Gets all the packages that are reachable from the 'tail" of this package:
    /// this is the transitive closure of the <see cref="ContentReachablePackages"/>.
    /// </summary>
    public IReadOnlySet<ResPackage> AllContentReachablePackages
    {
        get
        {
            EnsureContent();
            return _allContentReachablePackages;
        }
    }

    /// <summary>
    /// Gets whether at least one of the <see cref="AllContentReachablePackages"/> is a local package.
    /// </summary>
    public bool AllContentReachableHasLocalPackage
    {
        get
        {
            EnsureContent();
            return _allContentReachableHasLocalPackage;
        }
    }

    /// <summary>
    /// Gets the <see cref="FullName"/> (type name if this package is defined by a type).
    /// </summary>
    /// <returns>The package full name.</returns>
    public override string ToString() => ToString( _fullName, _type );

    internal static string ToString( string fullName, Type? type )
    {
        return type != null
                ? $"{fullName} ({type.Name})"
                : fullName;
    }
}
