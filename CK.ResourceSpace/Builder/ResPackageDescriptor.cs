using CK.EmbeddedResources;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace CK.Core;

/// <summary>
/// Mutable package descriptor.
/// </summary>
public sealed partial class ResPackageDescriptor : IDependentItemContainerTyped, IDependentItemContainerRef
{
    readonly ResPackageDescriptorContext _context;
    readonly string _fullName;
    readonly Type? _type;
    readonly NormalizedPath _defaultTargetPath;
    readonly StoreContainer _resources;
    readonly StoreContainer _afterResources;
    List<object>? _singleMappings;
    Ref _package;
    List<Ref>? _requires;
    List<Ref>? _requiredBy;
    List<Ref>? _groups;
    List<Ref>? _children;
    bool _isGroup;
    // Set by the CoreCollector during topological sort.
    internal bool _isOptional;

    internal ResPackageDescriptor( ResPackageDescriptorContext context,
                                   string fullName,
                                   Type? type,
                                   NormalizedPath defaultTargetPath,
                                   StoreContainer resources,
                                   StoreContainer afterResources )
    {
        Throw.DebugAssert( resources != afterResources );
        _context = context;
        _fullName = fullName;
        _type = type;
        _defaultTargetPath = defaultTargetPath;
        _resources = resources;
        _afterResources = afterResources;
    }

    /// <summary>
    /// Gets this package full name. When built from a type, this is the type's full name.
    /// </summary>
    public string FullName => _fullName;

    /// <summary>
    /// Gets the type if this package is defined by a type.
    /// </summary>
    public Type? Type => _type;

    /// <summary>
    /// Gets whether this is a local package: its <see cref="Resources"/> or <see cref="AfterResources"/>
    /// is a <see cref="FileSystemResourceContainer"/> with a true <see cref="FileSystemResourceContainer.HasLocalFilePathSupport"/>.
    /// </summary>
    public bool IsLocalPackage => _resources.LocalPath != null || _afterResources.LocalPath != null;

    /// <summary>
    /// Gets whether this package has been registered as an optional one.
    /// When this is still true after <see cref="ResSpaceDataBuilder.Build(IActivityMonitor)"/> has run,
    /// this package doesn't belong to the final <see cref="ResSpaceData"/>.
    /// </summary>
    public bool IsOptional => _isOptional;

    /// <summary>
    /// Gets the "Res" resources for this package.
    /// </summary>
    public IResourceContainer Resources => _resources;

    internal IResourceContainer ResourcesInnerContainer => _resources.InnerContainer;

    /// <summary>
    /// Gets the "Res[After]" resources for this package.
    /// They apply after this package's <see cref="Children"/>.
    /// </summary>
    public IResourceContainer AfterResources => _afterResources;

    internal IResourceContainer AfterResourcesInnerContainer => _afterResources.InnerContainer;

    /// <summary>
    /// Removes a resource that must belong to <see cref="Resources"/> or <see cref="AfterResources"/>
    /// from the stored resources (strictly speaking, the resource is "hidden").
    /// The same resource can be removed more than once: the resource is searched in the actual container. 
    /// <para>
    /// This enable code generators to take control of a resource that they want to handle directly.
    /// The resource will no more appear in the final stores and won't be handled by
    /// <see cref="ResourceSpaceFolderHandler"/> and <see cref="ResourceSpaceFileHandler"/>.
    /// </para>
    /// <para>
    /// How the removed resource is "transferred" (or not) in the <see cref="ResSpaceCollector.GeneratedCodeContainer"/>
    /// is up to the code generators.
    /// </para>
    /// </summary>
    /// <param name="resource">The resource to remove from stores.</param>
    public void RemoveCodeHandledResource( ResourceLocator resource )
    {
        Throw.CheckArgument( resource.Container == Resources || resource.Container == AfterResources );
        _context.RegisterCodeHandledResources( resource );
    }

    /// <summary>
    /// Finds the <paramref name="resourceName"/> that must exist in <see cref="Resources"/> or <see cref="AfterResources"/>
    /// and calls <see cref="RemoveCodeHandledResource(ResourceLocator)"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resourceName">The resource name to find.</param>
    /// <param name="resource">The found resource.</param>
    /// <returns>True on success, false if the resource cannot be found (an error is logged).</returns>
    public bool RemoveExpectedCodeHandledResource( IActivityMonitor monitor, string resourceName, out ResourceLocator resource )
    {
        if( !_resources.InnerContainer.TryGetExpectedResource( monitor, resourceName, out resource, _afterResources.InnerContainer ) )
        {
            return false;
        }
        _context.RegisterCodeHandledResources( resource );
        return true;
    }

    /// <summary>
    /// Finds the <paramref name="resourceName"/> that may exist in <see cref="Resources"/> or <see cref="AfterResources"/>
    /// and calls <see cref="RemoveCodeHandledResource(ResourceLocator)"/>.
    /// </summary>
    /// <param name="resourceName">The resource name to find.</param>
    /// <param name="resource">The found (and removed) resource.</param>
    /// <returns>True if the resource has been found and removed, false otherwise.</returns>
    public bool RemoveCodeHandledResource( string resourceName, out ResourceLocator resource )
    {
        if( _resources.InnerContainer.TryGetResource( resourceName, out resource )
            || _afterResources.InnerContainer.TryGetResource( resourceName, out resource ) )
        {
            _context.RegisterCodeHandledResources( resource );
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a mapping from a name to this package descriptor.
    /// The <paramref name="alias"/> must not be already associated to a package descriptor
    /// otherwise an error is logged and false is returned.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="alias">The name to map to this descriptor.</param>
    /// <returns>True on success, false on error.</returns>
    public bool AddSingleMapping( IActivityMonitor monitor, string alias )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( alias );
        if( _context.AddSingleMapping( monitor, alias, this ) )
        {
            _singleMappings ??= new List<object>();
            _singleMappings.Add( alias );
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a mapping from a type to this package descriptor:
    /// <list type="number">
    ///     <item><see cref="Type"/> must not be null.</item>
    ///     <item>The <paramref name="alias"/> must be assignable from <see cref="Type"/>.</item>
    ///     <item>The <paramref name="alias"/> must not be already associated to a package descriptor.</item>
    /// </list>
    /// If any of these conditions is not met, an error is logged and false is returned.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="alias">The type to map to this descriptor.</param>
    /// <returns>True on success, false on error.</returns>
    public bool AddSingleMapping( IActivityMonitor monitor, Type alias )
    {
        Throw.CheckNotNullArgument( alias );
        if( _context.AddSingleMapping( monitor, alias, this ) )
        {
            _singleMappings ??= new List<object>();
            _singleMappings.Add( alias );
            return true;
        }
        return false;
    }

    internal IReadOnlyList<object>? SingleMappings => _singleMappings;

    /// <summary>
    /// Gets the default target path that will prefix resources that are items.
    /// </summary>
    public NormalizedPath DefaultTargetPath => _defaultTargetPath;

    /// <summary>
    /// Gets or sets whether this is a group instead of a regular package.
    /// <para>
    /// Defaults to false.
    /// </para>
    /// </summary>
    public bool IsGroup { get => _isGroup; set => _isGroup = value; }

    /// <summary>
    /// Gets or sets a <see cref="Ref"/> to the package that owns this one.
    /// <para>
    /// Defaults to the <c>default</c> value (<see cref="Ref.IsValid"/> is false): there
    /// is no owner.
    /// </para>
    /// <para>
    /// Before calling <see cref="ResSpaceDataBuilder.Build(IActivityMonitor)"/>, <see cref="IsGroup"/> must
    /// be false otherwise this will be an error.
    /// </para>
    /// </summary>
    public Ref Package { get => _package; set => _package = value; }

    /// <summary>
    /// Gets a mutable list of requirements that can be optional references.
    /// </summary>
    public IList<Ref> Requires => _requires ??= new List<Ref>();

    /// <summary>
    /// Gets a mutable list of revert dependencies (a package can specify that it is itself required by another one). 
    /// Often, a "RequiredBy" constraint should be specified as <see cref="Ref.IsOptional"/>.
    /// </summary>
    public IList<Ref> RequiredBy => _requiredBy ??= new List<Ref>();

    /// <summary>
    /// Gets a mutable list of children.
    /// </summary>
    public IList<Ref> Children => _children ??= new List<Ref>();

    /// <summary>
    /// Gets a mutable list of groups to which this item belongs. If one of these groups is a package,
    /// it must be the only package of this item (otherwise it is an error).
    /// </summary>
    public IList<Ref> Groups => _groups ??= new List<Ref>();

    internal bool CheckContext( IActivityMonitor monitor, string relName, ResPackageDescriptorContext expected )
    {
        if( _context != expected )
        {
            monitor.Error( $"'{relName}' relationship context mismatch. The package '{ToString()}' belongs to another collector." );
            return false;
        }
        return true;
    }


    internal bool Initialize( IActivityMonitor monitor, IReadOnlyDictionary<object, ResPackageDescriptor> packageIndex )
    {
        bool success = InitializeConfiguredDependencies( monitor, packageIndex );
        if( _type != null )
        {
            // Detect a useless CKPackage.xml for the type: currently, there's
            // no "merge" possible, the type drives.
            var descriptor = _resources.GetResource( "CKPackage.xml" );
            if( descriptor.IsValid )
            {
                monitor.Warn( $"Found {descriptor} for type '{_type:N}'. Ignored." );
            }
        }
        return success;
    }

    bool InitializeConfiguredDependencies( IActivityMonitor monitor, IReadOnlyDictionary<object, ResPackageDescriptor> packageIndex )
    {
        bool success = true;
        if( _package.IsValid )
        {
            _package = ResolveRef( monitor, "Package", _package, _context, packageIndex );
            success &= _package.IsValid || _package.IsOptional;
        }
        success &= ResolveReferences( monitor, "Requires", _requires, _context, packageIndex );
        success &= ResolveReferences( monitor, "RequiredBy", _requiredBy, _context, packageIndex );
        success &= ResolveReferences( monitor, "Groups", _groups, _context, packageIndex );
        success &= ResolveReferences( monitor, "Children", _children, _context, packageIndex );
        return success;

        static bool ResolveReferences( IActivityMonitor monitor,
                                       string relName,
                                       List<Ref>? list,
                                       ResPackageDescriptorContext context,
                                       IReadOnlyDictionary<object, ResPackageDescriptor> packageIndex )
        {
            if( list == null ) return true;
            bool success = true;
            for( int i = 0; i < list.Count; i++ )
            {
                var r = list[i];
                if( r.IsValid )
                {
                    r = ResolveRef( monitor, relName, r, context, packageIndex );
                    if( r.IsValid )
                    {
                        list[i] = r;
                    }
                    else
                    {
                        if( r.IsOptional )
                        {
                            list.RemoveAt( i-- );
                        }
                        else
                        {
                            success = false;
                        }
                    }
                }
                else
                {
                    list.RemoveAt( i-- );
                }
            }
            return success;
        }

        static Ref ResolveRef( IActivityMonitor monitor,
                               string relName,
                               Ref r,
                               ResPackageDescriptorContext context,
                               IReadOnlyDictionary<object, ResPackageDescriptor> packageIndex )
        {
            Throw.DebugAssert( r.IsValid );
            var p = r.AsPackageDescriptor;
            if( p != null )
            {
                return CheckContext( monitor, relName, context, p ) ? r : default;
            }
            if( packageIndex.TryGetValue( r._ref, out var result ) )
            {
                return result;
            }
            if( r.IsOptional )
            {
                monitor.Warn( $"'{relName}' optional reference '{r}' resolution failed. Skipped." );
                return Ref.OptionalInvalid;
            }
            monitor.Error( $"'{relName}' resolution failed. The reference '{r}' is not registered." );
            return Ref.Invalid;
        }

        static bool CheckContext( IActivityMonitor monitor, string relName, ResPackageDescriptorContext context, ResPackageDescriptor p )
        {
            if( p._context != context )
            {
                monitor.Error( $"'{relName}' relationship context mismatch. The package '{p}' belongs to another collector." );
                return false;
            }
            return true;
        }

    }

    DependentItemKind IDependentItemContainerTyped.ItemKind => _isGroup ? DependentItemKind.Group : DependentItemKind.Container;

    IDependentItemContainerRef? IDependentItem.Container => _package.AsPackageDescriptor;

    // We don't use the Optional feature of th CK.SetupDependency model: optionality
    // is managed here, at the PackageDescriptor level and this is for the best.
    // This feature should be removed from CK.SetupDependency or a new, simpler package
    // should be created without optionality and registration features that complicates
    // (a lot) its implementation.
    sealed class Adapter : IDependentItemGroupRef
    {
        readonly string _fullName;
        public Adapter( Ref r )
        {
            Throw.DebugAssert( r.IsValid );
            _fullName = r.FullName;
        }
        public string FullName => _fullName;
        public bool Optional => false;
    }

    IEnumerable<IDependentItemRef>? IDependentItem.Requires => _requires?.Select( r => new Adapter( r ) );

    IEnumerable<IDependentItemRef>? IDependentItem.RequiredBy => _requiredBy?.Select( r => new Adapter( r ) );

    IEnumerable<IDependentItemRef>? IDependentItemGroup.Children => _children?.Select( r => new Adapter( r ) );

    IDependentItemRef? IDependentItem.Generalization => null;

    IEnumerable<IDependentItemGroupRef>? IDependentItem.Groups => _groups?.Select( r => new Adapter( r ) );

    bool IDependentItemRef.Optional => false;

    object? IDependentItem.StartDependencySort( IActivityMonitor m ) => null;

    /// <inheritdoc cref="ResPackage.ToString()"/>
    public override string ToString() => ResPackage.ToString( _fullName, _type );

}
