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
public sealed class ResPackageDescriptor : IDependentItemContainerTyped, IDependentItemContainerRef
{
    readonly IReadOnlyDictionary<object,ResPackageDescriptor> _collector;
    readonly string _fullName;
    readonly Type? _type;
    readonly NormalizedPath _defaultTargetPath;
    readonly CodeStoreResources _resources;
    readonly CodeStoreResources _afterResources;
    readonly string? _localPath;
    ResPackageDescriptor? _package;
    List<ResPackageDescriptor>? _requires;
    List<ResPackageDescriptor>? _requiredBy;
    List<ResPackageDescriptor>? _groups;
    List<ResPackageDescriptor>? _children;
    bool _isGroup;

    internal ResPackageDescriptor( IReadOnlyDictionary<object, ResPackageDescriptor> collector,
                                   string fullName,
                                   Type? type,
                                   NormalizedPath defaultTargetPath,
                                   CodeStoreResources resources,
                                   CodeStoreResources afterResources,
                                   string? localPath )
    {
        Throw.DebugAssert( resources != afterResources );
        _collector = collector;
        _fullName = fullName;
        _type = type;
        _defaultTargetPath = defaultTargetPath;
        _resources = resources;
        _afterResources = afterResources;
        _localPath = localPath;
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
    /// Gets a non null fully qualified path of this package's resources if this is a local package.
    /// </summary>
    public string? LocalPath => _localPath;

    /// <summary>
    /// Gets the <see cref="CodeStoreResources"/> for this package.
    /// </summary>
    public CodeStoreResources Resources => _resources;

    /// <summary>
    /// Gets the <see cref="CodeStoreResources"/> that apply after this package's <see cref="Children"/>.
    /// </summary>
    public CodeStoreResources AfterResources => _afterResources;

    /// <summary>
    /// Gets the default target path that will prefix resources that are items.
    /// </summary>
    public NormalizedPath DefaultTargetPath => _defaultTargetPath;

    /// <summary>
    /// Gets or sets whether this package is a group.
    /// </summary>
    public bool IsGroup { get => _isGroup; set => _isGroup = value; }

    /// <summary>
    /// Gets or sets the package that owns this one.
    /// <para>
    /// It must belong to the same collector otherwise an <see cref="ArgumentException"/> is thrown.
    /// Before calling <see cref="ResourceSpaceDataBuilder.Build(IActivityMonitor)"/>, <see cref="IsGroup"/> must
    /// be false otherwise this will be an error.
    /// </para>
    /// </summary>
    public ResPackageDescriptor? Package
    {
        get => _package;
        set
        {
            if( value != null )
            {
                if( value._collector != _collector )
                {
                    Throw.ArgumentException( nameof( value ), $"Package mismatch. The package '{value.FullName}' belongs to another collector." );
                }
            }
            _package = value;
        }

    }

    /// <summary>
    /// Gets a mutable list of requirements that can be named optional references.
    /// </summary>
    public IList<ResPackageDescriptor> Requires => _requires ??= new List<ResPackageDescriptor>();

    /// <summary>
    /// Gets a mutable list of revert dependencies (a package can specify that it is itself required by another one). 
    /// A "RequiredBy" constraint is optional: a missing "RequiredBy" is not an error (it is considered 
    /// as a reverted optional dependency).
    /// </summary>
    public IList<ResPackageDescriptor> RequiredBy => _requiredBy ??= new List<ResPackageDescriptor>();

    /// <summary>
    /// Gets a mutable list of children.
    /// </summary>
    public IList<ResPackageDescriptor> Children => _children ??= new List<ResPackageDescriptor>();

    /// <summary>
    /// Gets a mutable list of groups to which this item belongs. If one of these groups is a container,
    /// it must be the only container of this item (otherwise it is an error).
    /// </summary>
    public IList<ResPackageDescriptor> Groups => _groups ??= new List<ResPackageDescriptor>();

    internal bool InitializeFromType( IActivityMonitor monitor )
    {
        Throw.DebugAssert( _type != null );
        bool success = true;

        var attributes = _type.GetCustomAttributes( inherit: false );
        var genAttributes = attributes.Where( a => a.GetType().IsGenericType )
                                      .Select( a => (Attribute: a,
                                                     GenType: a.GetType().GetGenericTypeDefinition(),
                                                     GenArgs: a.GetType().GetGenericArguments()) );

        HandlePackage( monitor, ref success, attributes, genAttributes );

        // No error frm now on, only warnings.
        // Starts with the type: any duplicate is necessarily from a generic attribute parameter.
        HandleMultiType( monitor,
                         genAttributes,
                         ref _requires,
                         "Requires",
                         typeof( RequiresAttribute<> ), typeof( RequiresAttribute<,> ), typeof( RequiresAttribute<,,> ),
                         typeof( RequiresAttribute<,,,> ), typeof( RequiresAttribute<,,,,> ), typeof( RequiresAttribute<,,,,,> ) );
        HandleMultiType( monitor,
                         genAttributes,
                         ref _requiredBy,
                         "RequiredBy",
                         typeof( RequiredByAttribute<> ), typeof( RequiredByAttribute<,> ), typeof( RequiredByAttribute<,,> ),
                         typeof( RequiredByAttribute<,,,> ), typeof( RequiredByAttribute<,,,,> ), typeof( RequiredByAttribute<,,,,,> ) );
        HandleMultiType( monitor,
                         genAttributes,
                         ref _groups,
                         "Groups",
                         typeof( GroupsAttribute<> ), typeof( GroupsAttribute<,> ), typeof( GroupsAttribute<,,> ),
                         typeof( GroupsAttribute<,,,> ), typeof( GroupsAttribute<,,,,> ), typeof( GroupsAttribute<,,,,,> ) );
        HandleMultiType( monitor,
                         genAttributes,
                         ref _children,
                         "Children",
                         typeof( ChildrenAttribute<> ), typeof( ChildrenAttribute<,> ), typeof( ChildrenAttribute<,,> ),
                         typeof( ChildrenAttribute<,,,> ), typeof( ChildrenAttribute<,,,,> ), typeof( ChildrenAttribute<,,,,,> ) );

        // Then handles names. Duplicates can be differentiated.
        var req = attributes.OfType<RequiresAttribute>().FirstOrDefault();
        if( req != null )
        {
            HandleMultiName( monitor, req.CommaSeparatedPackageFullnames, ref _requires, "Requires" );
        }
        var reqBy = attributes.OfType<RequiredByAttribute>().FirstOrDefault();
        if( reqBy != null )
        {
            HandleMultiName( monitor, reqBy.CommaSeparatedPackageFullnames, ref _requiredBy, "RequiredBy" );
        }
        var groups = attributes.OfType<GroupsAttribute>().FirstOrDefault();
        if( groups != null )
        {
            HandleMultiName( monitor, groups.CommaSeparatedPackageFullnames, ref _groups, "Groups" );
        }
        var children = attributes.OfType<ChildrenAttribute>().FirstOrDefault();
        if( children != null )
        {
            HandleMultiName( monitor, children.CommaSeparatedPackageFullnames, ref _children, "Children" );
        }

        return success;
    }

    void HandleMultiName( IActivityMonitor monitor,
                          string[] commaSeparatedPackageFullnames,
                          ref List<ResPackageDescriptor>? list, string relName )
    {
        foreach( var n in commaSeparatedPackageFullnames )
        {
            foreach( var name in n.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries ) )
            {
                if( !_collector.TryGetValue( name, out var package ) )
                {
                    monitor.Warn( $"[{relName}( \"{name}\" )] on type '{_type:N}' skipped as target full name is not registered in this ResourceSpace." );
                }
                else
                {
                    if( list == null )
                    {
                        list = new List<ResPackageDescriptor> { package };
                    }
                    else
                    {
                        if( list.Contains( package ) )
                        {
                            if( package.Type == null )
                            {
                                monitor.Warn( $"Duplicate '[{relName}( \"{name}\" )]' on type '{_type:N}'. Ignored." );
                            }
                            else
                            {
                                monitor.Warn( $"Duplicate '[{relName}( \"{name}\" )]' on type '{_type:N}'. Already defined by one '[{relName}<{package.Type:N}>]'. Ignored." );
                            }
                        }
                        else
                        {
                            list.Add( package );
                        }
                    }
                }
            }
        }
    }

    void HandlePackage( IActivityMonitor monitor,
                        ref bool success,
                        object[] attributes,
                        IEnumerable<(object Attribute, Type GenType, Type[] GenArgs)> genAttributes )
    {
        var packageNAttr = attributes.OfType<PackageAttribute>().FirstOrDefault();
        if( packageNAttr != null )
        {
            if( !_collector.TryGetValue( packageNAttr.PackageFullName, out var package ) )
            {
                monitor.Warn( $"[Package( \"{packageNAttr.PackageFullName}\" )] on type '{_type:N}' skipped as target full name is not registered in this ResourceSpace." );
            }
            else
            {
                _package = package;
            }
        }
        var packageTAttr = genAttributes.FirstOrDefault( a => a.GenType == typeof( PackageAttribute<> ) ).GenArgs?[0];
        if( packageTAttr != null )
        {
            if( packageNAttr != null )
            {
                monitor.Error( $"Only one of [Package<{packageTAttr:N}>] and [Package( \"{packageNAttr.PackageFullName}\" )] can decorate type '{_type:N}'." );
                success = false;
            }
            else
            {
                if( !_collector.TryGetValue( packageTAttr, out var package ) )
                {
                    monitor.Warn( $"[Package<{packageTAttr:N}>] on type '{_type:N}' skipped as type target is not registered in this ResourceSpace." );
                }
                _package = package;
            }
        }
    }

    void HandleMultiType( IActivityMonitor monitor,
                          IEnumerable<(object Attribute, Type GenType, Type[] GenArgs)> genAttributes,
                          ref List<ResPackageDescriptor>? list,
                          string relName,
                          params Type[] genTypes )
    {
        foreach( var t in genAttributes.Where( a => genTypes.Contains( a.GenType ) ) )
        {
            if( !_collector.TryGetValue( t, out var package ) )
            {
                monitor.Warn( $"[{relName}<{t:N}>] on type '{_type:N}' skipped as type target is not registered in this ResourceSpace." );
            }
            else
            {
                if( list == null )
                {
                    list = new List<ResPackageDescriptor> { package };
                }
                else
                {
                    if( list.Contains( package ) )
                    {
                        monitor.Warn( $"Duplicate '[{relName}<{t:N}>]' on type '{_type:N}'. Ignored." );
                    }
                    else
                    {
                        list.Add( package );
                    }
                }
            }

        }
    }

    internal void InitializeFromPackageDescriptor( IActivityMonitor monitor, XmlReader xmlReader )
    {
        throw new NotImplementedException();
    }

    DependentItemKind IDependentItemContainerTyped.ItemKind => _isGroup ? DependentItemKind.Group : DependentItemKind.Container;

    IDependentItemContainerRef? IDependentItem.Container => _package;

    IEnumerable<IDependentItemRef>? IDependentItem.Requires => _requires;

    IEnumerable<IDependentItemRef>? IDependentItem.RequiredBy => _requiredBy;

    IEnumerable<IDependentItemRef>? IDependentItemGroup.Children => _children;

    IDependentItemRef? IDependentItem.Generalization => null;

    IEnumerable<IDependentItemGroupRef>? IDependentItem.Groups => _groups;

    bool IDependentItemRef.Optional => false;

    object? IDependentItem.StartDependencySort( IActivityMonitor m ) => null;

    /// <inheritdoc cref="ResPackage.ToString()"/>
    public override string ToString() => ResPackage.ToString( _fullName, _type );

}
