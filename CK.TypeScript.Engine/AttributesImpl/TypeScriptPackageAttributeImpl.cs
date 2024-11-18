using CK.Core;
using CK.Setup;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace CK.TypeScript.Engine;

/// <summary>
/// Implementation class of the <see cref="TypeScriptPackageAttribute"/>.
/// <para>
/// This must be used as the base class of specialized TypeScriptPackageAttribute implementations.
/// </para>
/// </summary>
public class TypeScriptPackageAttributeImpl : IAttributeContextBoundInitializer, IStObjStructuralConfigurator
{
    readonly TypeScriptPackageAttribute _attr;
    readonly Type _type;
    readonly IResourceContainer _resources;
    readonly HashSet<Core.ResourceLocator> _removedResources;
    readonly List<TypeScriptPackageAttributeImplExtension> _extensions;
    NormalizedPath _typeScriptFolder;
    TSLocaleCultureSet? _tsLocales;
    // This is here only to support RegisterTypeScriptType registration...
    // This is bad and must be refactored.
    [AllowNull] ITypeAttributesCache _owner;


    /// <summary>
    /// Gets the folder of this package. Defaults to the namespace of the decorated type unless
    /// specified by <see cref="TypeScriptPackageAttribute.TypeScriptFolder"/>.
    /// </summary>
    public NormalizedPath TypeScriptFolder => _typeScriptFolder;

    /// <summary>
    /// Enables specializations to specify the <see cref="TypeScriptFolder"/>.
    /// </summary>
    /// <param name="folder">The new folder.</param>
    protected void SetTypeScriptFolder( NormalizedPath folder ) => _typeScriptFolder = folder;

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public TypeScriptPackageAttribute Attribute => _attr;

    /// <summary>
    /// Gets the decorated type.
    /// </summary>
    public Type DecoratedType => _type;

    /// <summary>
    /// Gets the resources for this package.
    /// </summary>
    public IResourceContainer Resources => _resources;

    /// <summary>
    /// Gets the local culture set that contains the translations associated to this package.
    /// </summary>
    public TSLocaleCultureSet? TSLocales => _tsLocales;

    /// <summary>
    /// Initializes a new <see cref="TypeScriptPackageAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public TypeScriptPackageAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type )
    {
        _attr = attr;
        _type = type;
        if( !typeof( TypeScriptPackage ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[TypeScriptPackage] can only decorate a TypeScriptPackage: '{type:N}' is not a TypeScriptPackage." );
        }

        _extensions = new List<TypeScriptPackageAttributeImplExtension>();
        _removedResources = new HashSet<Core.ResourceLocator>();
        // Computes Resources: if an error occured IsValid is false and a error has been logged that stops the processing.
        _resources = type.Assembly.GetResources().CreateResourcesContainerForType( monitor, attr.CallerFilePath, type, "TypeScriptPackage" );

        // Initializes TypeScriptFolder.
        Throw.DebugAssert( type.Namespace != null );
        if( string.IsNullOrWhiteSpace( attr.TypeScriptFolder ) )
        {
            _typeScriptFolder = type.Namespace.Replace( '.', '/' );
        }
        else
        {
            _typeScriptFolder = new NormalizedPath( attr.TypeScriptFolder );
            if( _typeScriptFolder.IsRooted )
            {
                monitor.Warn( $"[TypeScriptPackage] on '{type:C}': TypeScriptFolder is rooted, this is useless and removed." );
                _typeScriptFolder = _typeScriptFolder.With( NormalizedPathRootKind.None );
            }
        }
    }

    void IAttributeContextBoundInitializer.Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        _owner = owner;
    }

    internal void AddExtension( TypeScriptPackageAttributeImplExtension e )
    {
        _extensions.Add( e );
    }

    void IStObjStructuralConfigurator.Configure( IActivityMonitor monitor, IStObjMutableItem o )
    {
        if( _attr.Package != null )
        {
            if( o.Container.Type == null ) o.Container.Type = _attr.Package;
            else if( o.Container.Type != _attr.Package )
            {
                monitor.Error( $"{o.ToString()}: [TypeScriptPackage] sets Package to be '{_attr.Package.Name}' but it is already '{o.Container.Type:N}'." );
            }
        }
        else if( !typeof( RootTypeScriptPackage ).IsAssignableFrom( o.ClassType ) )
        {
            o.Container.Type = typeof( RootTypeScriptPackage );
            o.Container.StObjRequirementBehavior = StObjRequirementBehavior.None;
        }
        // Keeps the potentially configured item kind (allows TypeScriptPackages to be Group rather than Container).
        if( o.ItemKind == DependentItemKindSpec.Unknown )
        {
            o.ItemKind = DependentItemKindSpec.Container;
        }
        OnConfigure( monitor, o );
    }

    /// <summary>
    /// See <see cref="IStObjStructuralConfigurator.Configure(IActivityMonitor, IStObjMutableItem)"/>.
    /// If <see cref="TypeScriptPackageAttribute.Package"/> is not null, it has already been set on <see cref="IStObjMutableItem.Container"/>:
    /// at this level, this does nothing.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="o">The mutable item.</param>
    protected virtual void OnConfigure( IActivityMonitor monitor, IStObjMutableItem o )
    {
    }

    /// <summary>
    /// Called once the <see cref="ITSCodeGeneratorFactory"/> have created their <see cref="ITSCodeGenerator"/> in the order
    /// of the topological sort of the <see cref="TypeScriptPackage"/>.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="initializer">The TypeScriptContext initializer.</param>
    /// <returns>True on success, false otherwise (errors must be logged).</returns>
    internal bool InitializeTypeScriptPackage( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        bool success = true;

        // First, initializes our _tsLocales if a "ts-locales/" folder exists.
        // And if we have locales, remove them from the resources as they are handled separately.
        Throw.DebugAssert( _resources.IsValid );
        success &= TSLocaleCultureSet.LoadTSLocales( monitor, _resources, initializer.BinPathConfiguration.ActiveCultures, out _tsLocales );
        if( _tsLocales != null )
        {
            _removedResources.AddRange( _resources.AllResources.Where( r => r.LocalResourceName.Span.StartsWith( "ts-locales" ) ) );
        }

        // Then handle the RegisterTypeScriptTypeAttribute.
        foreach( var r in _owner.GetTypeCustomAttributes<RegisterTypeScriptTypeAttribute>() )
        {
            success &= initializer.EnsureRegister( monitor, r.Type, false, attr =>
            {
                // A Register can override because of package ordering...
                // Even if this is weird.
                if( attr != null )
                {
                    if( r.TypeName != attr.TypeName
                       || r.FileName != attr.FileName
                       || r.Folder != attr.Folder
                       || r.SameFileAs != attr.SameFileAs
                       || r.SameFolderAs != attr.SameFolderAs )
                    {
                        monitor.Warn( $"[RegisterTypeScriptType] on '{_owner.Type:N}' overrides current '{r.Type:C}' configuration." );
                        return attr.ApplyOverride( r );
                    }
                    return attr;
                }
                return new TypeScriptAttribute( r );
            } );
        }
        return success;
    }

    /// <summary>
    /// Removes a resource from the <see cref="Resources"/>: <see cref="GenerateCode(IActivityMonitor, TypeScriptContext)"/>
    /// won't generate its file.
    /// </summary>
    /// <param name="resource"></param>
    internal protected void RemoveResource( Core.ResourceLocator resource )
    {
        Throw.DebugAssert( resource.Container == _resources );
        _removedResources.Add( resource );
    }

    /// <summary>
    /// Called in the order of the topological sort of the <see cref="TypeScriptPackage"/>.
    /// <para>
    /// At this level, if <see cref="TypeScriptPackageAttribute.ConsiderExplicitResourceOnly"/> is false (the default), embedded resources
    /// are copied to the <see cref="TypeScriptFolder"/>.
    /// If <see cref="TSLocales"/> exists, it is added to the <see cref="TypeScriptContext.TSLocales"/>.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="context">The TypeScriptContext.</param>
    /// <returns>True on success, false otherwise (errors must be logged).</returns>
    internal protected virtual bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context )
    {
        bool success = true;

        if( _tsLocales != null ) context.TSLocales.Add( _tsLocales );

        foreach( var e in _extensions )
        {
            success &= e.GenerateCode( monitor, this, context );
        }
        if( !_attr.ConsiderExplicitResourceOnly )
        {
            foreach( var r in _resources.AllResources )
            {
                if( _removedResources.Contains( r ) ) continue;
                var targetFileName = _typeScriptFolder.Combine( r.LocalResourceName.ToString() );
                context.Root.Root.CreateResourceFile( r, targetFileName );
            }
        }
        return true;
    }

}
