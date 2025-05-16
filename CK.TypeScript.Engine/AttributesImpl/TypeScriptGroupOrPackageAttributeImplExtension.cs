using CK.Core;
using CK.Setup;
using System;
using System.Linq;
using System.Reflection;

namespace CK.TypeScript.Engine;

/// <summary>
/// Base class for associated attributes to a <c>[TypeScriptPackage]</c> (a <see cref="TypeScriptPackageAttribute"/>
/// or a specialization).
/// </summary>
public abstract class TypeScriptGroupOrPackageAttributeImplExtension : IAttributeContextBoundInitializer, ITSCodeGeneratorAutoDiscovery
{
    readonly Attribute _attr;
    readonly Type _type;

    /// <summary>
    /// Initializes a new implementation.
    /// </summary>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    protected TypeScriptGroupOrPackageAttributeImplExtension( Attribute attr, Type type )
    {
        _attr = attr;
        _type = type;
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public Attribute Attribute => _attr;

    /// <summary>
    /// Gets the decorated type.
    /// </summary>
    public Type Type => _type;

    /// <summary>
    /// Gets the attribute name without "Attribute" suffix.
    /// </summary>
    public ReadOnlySpan<char> AttributeName
    {
        get
        {
            Throw.DebugAssert( _attr.GetType().Name.EndsWith( "Attribute" ) && "Attribute".Length == 9 );
            return _attr.GetType().Name.AsSpan( 0..^9 );
        }
    }

    void IAttributeContextBoundInitializer.Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        var tsPackage = owner.GetTypeCustomAttributes<TypeScriptGroupOrPackageAttributeImpl>().SingleOrDefault();
        if( tsPackage == null )
        {
            monitor.Error( $"[{AttributeName}] on '{owner.Type:N}' requires the [TypeScriptPackage] (or a specialization) to also be declared." );
        }
        else
        {
            tsPackage.AddExtension( this );
        }
    }

    /// <summary>
    /// Called by the <see cref="TypeScriptGroupOrPackageAttributeImpl.CreateResPackageDescriptor(IActivityMonitor, TypeScriptContext, ResSpaceConfiguration)"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The TypeScript context.</param>
    /// <param name="tsPackage">The package attribute.</param>
    /// <param name="d">The package to configure.</param>
    /// <param name="resourcesConfiguration">The package's configuration.</param>
    /// <returns>Must return true on success, false on error (errors must be logged).</returns>
    internal protected abstract bool OnConfiguredDescriptor( IActivityMonitor monitor,
                                                             TypeScriptContext context,
                                                             TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                             ResPackageDescriptor d,
                                                             ResSpaceConfiguration resourcesConfiguration );

    internal protected virtual bool OnResPackageAvailable( IActivityMonitor monitor,
                                                           TypeScriptContext context,
                                                           TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                           ResPackage resPackage )
    {
        return true;
    }

    internal protected virtual bool OnResSpaceAvailable( IActivityMonitor monitor,
                                                         TypeScriptContext context,
                                                         TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                         ResPackage resPackage,
                                                         ResSpace resSpace )
    {
        return true;
    }

}
