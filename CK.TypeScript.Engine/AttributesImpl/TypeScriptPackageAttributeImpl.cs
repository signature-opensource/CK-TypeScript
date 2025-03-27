using CK.Core;
using CK.EmbeddedResources;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    readonly List<TypeScriptPackageAttributeImplExtension> _extensions;
    NormalizedPath _typeScriptFolder;
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
    /// Initializes a new <see cref="TypeScriptPackageAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
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
    /// of the topological sort of the <see cref="TypeScriptPackage"/>. A package is initialized after its subordinated
    /// packages (its content).
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="initializer">The TypeScriptContext initializer.</param>
    /// <returns>True on success, false otherwise (errors must be logged).</returns>
    internal bool InitializeTypeScriptPackage( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        bool success = true;

        // Handle the RegisterTypeScriptTypeAttribute.
        foreach( var r in _owner.GetTypeCustomAttributes<RegisterTypeScriptTypeAttribute>() )
        {
            success &= initializer.EnsureRegister( monitor, r.Type, mustBePocoType: false, attr =>
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
    /// Called in the order of the topological sort of the <see cref="TypeScriptPackage"/>. This is called after
    /// the subordinated packages (the package's content).
    /// <para>
    /// At this level, if <see cref="TypeScriptPackageAttribute.ConsiderExplicitResourceOnly"/> is false (the default), embedded resources
    /// are copied to the <see cref="TypeScriptFolder"/>.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="context">The TypeScriptContext.</param>
    /// <returns>True on success, false otherwise (errors must be logged).</returns>
    internal protected virtual bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context )
    {
        bool success = true;
        foreach( var e in _extensions )
        {
            success &= e.GenerateCode( monitor, this, context );
        }
        return success;
    }

}
