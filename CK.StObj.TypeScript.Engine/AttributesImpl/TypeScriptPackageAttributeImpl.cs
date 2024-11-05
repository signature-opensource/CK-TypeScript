using CK.Core;
using CK.Setup;
using System;

namespace CK.StObj.TypeScript.Engine;

/// <summary>
/// Implementation class of the <see cref="TypeScriptPackageAttribute"/>.
/// <para>
/// This must be used as the base class of specialized TypeScriptPackageAttribute implementations.
/// </para>
/// </summary>
public class TypeScriptPackageAttributeImpl : IAttributeContextBound, IStObjStructuralConfigurator
{
    readonly TypeScriptPackageAttribute _attr;
    readonly Type _type;
    readonly PackageResources _resources;
    NormalizedPath _typeScriptFolder;

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

    public PackageResources Resources => _resources;

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

        // Computes Resources: if an error occured this is null but we won't go further so we ignore the null here.
        _resources = PackageResources.Create( monitor, type, attr.ResourceFolderPath, attr.CallerFilePath, "TypeScriptPackage" )!;

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
    /// <para>
    /// Does nothing at this level.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="initializer">The TypeScriptContext iniitializer.</param>
    /// <returns>True on success, false otherwise (errors must be logged).</returns>
    internal protected virtual bool InitializePackage( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        return true;
    }

    /// <summary>
    /// Called in the order of the topological sort of the <see cref="TypeScriptPackage"/>.
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
        if( !_attr.ConsiderExplicitResourceOnly )
        {
            foreach( ResourceTypeLocator o in _resources.AllResources )
            {
                // Skip prefix 'ck@{_resourceTypeFolder}/'.
                var targetFileName = _typeScriptFolder.Combine( o.ResourceName.Substring( _resources.ResourcePrefix.Length ) );
                context.Root.Root.CreateResourceFile( o, targetFileName );
            }
        }
        return true;
    }

}
