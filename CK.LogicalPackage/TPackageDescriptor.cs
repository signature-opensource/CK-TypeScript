using CK.Core;
using CK.EmbeddedResources;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// 
/// </summary>
public sealed class TPackageDescriptor : IDependentItemContainerTyped, ITPackageDescriptorRef
{
    readonly string _fullName;
    readonly Type? _type;
    readonly NormalizedPath _defaultTargetPath;
    readonly IResourceContainer _packageResources;
    readonly CodeGenResourceContainer _codeGenResources;
    readonly string? _localPath;
    ITPackageDescriptorRef? _container;
    List<ITPackageDescriptorRef>? _requires;
    List<ITPackageDescriptorRef>? _requiredBy;
    List<ITPackageDescriptorRef>? _groups;
    List<ITPackageDescriptorRef>? _children;
    bool _isGroup;

    internal TPackageDescriptor( string fullName,
                                 Type? type,
                                 NormalizedPath defaultTargetPath,
                                 IResourceContainer packageResources,
                                 string? localPath )
    {
        _fullName = fullName;
        _type = type;
        _defaultTargetPath = defaultTargetPath;
        _packageResources = packageResources;
        _localPath = localPath;
        var n = packageResources is EmptyResourceContainer e ? e.NonDisabledDisplayName : packageResources.DisplayName;
        _codeGenResources = new CodeGenResourceContainer( $"[CodeGen] {n}" );
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
    /// Gets a mutable set of resources that can be code generated instead of being read from the resources.
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
    /// Gets the type if this package is defined by a type.
    /// </summary>
    public Type? Type => _type;

    /// <summary>
    /// Gets or sets whether this package is a group.
    /// </summary>
    public bool IsGroup { get => _isGroup; set => _isGroup = value; }

    /// <summary>
    /// Gets or sets the container for this item.
    /// </summary>
    public ITPackageDescriptorRef? Container
    {
        get => _container;
        set => _container = value;
    }

    /// <summary>
    /// Gets a mutable list of requirements that can be named optional references.
    /// </summary>
    public IList<ITPackageDescriptorRef> Requires => _requires ??= new List<ITPackageDescriptorRef>();

    /// <summary>
    /// Gets a mutable list of revert dependencies (a package can specify that it is itself required by another one). 
    /// A "RequiredBy" constraint is optional: a missing "RequiredBy" is not an error (it is considered 
    /// as a reverted optional dependency).
    /// </summary>
    public IList<ITPackageDescriptorRef> RequiredBy => _requiredBy ??= new List<ITPackageDescriptorRef>();

    /// <summary>
    /// Gets a mutable list of children.
    /// </summary>
    public IList<ITPackageDescriptorRef> Children => _children ??= new List<ITPackageDescriptorRef>();

    /// <summary>
    /// Gets a mutable list of groups to which this item belongs. If one of these groups is a container,
    /// it must be the only container of this item (otherwise it is an error).
    /// </summary>
    public IList<ITPackageDescriptorRef> Groups => _groups ??= new List<ITPackageDescriptorRef>();

    DependentItemKind IDependentItemContainerTyped.ItemKind => _isGroup ? DependentItemKind.Group : DependentItemKind.Container;

    IDependentItemContainerRef? IDependentItem.Container => _container;

    IEnumerable<IDependentItemRef>? IDependentItem.Requires => _requires;

    IEnumerable<IDependentItemRef>? IDependentItem.RequiredBy => _requiredBy;

    IEnumerable<IDependentItemRef>? IDependentItemGroup.Children => _children;

    IDependentItemRef? IDependentItem.Generalization => null;

    IEnumerable<IDependentItemGroupRef>? IDependentItem.Groups => _groups;

    bool IDependentItemRef.Optional => false;

    /// <summary>
    /// Gets the <see cref="FullName"/> (type name if this package is defined by a type).
    /// </summary>
    /// <returns>The package full name.</returns>
    public override string ToString()
    {
        return _type != null
                ? $"{_fullName} ({_type.Name})"
                : _fullName;
    }

    object? IDependentItem.StartDependencySort( IActivityMonitor m ) => null;
}
