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

    protected TypeScriptGroupOrPackageAttributeImplExtension( Attribute attr, Type type )
    {
        _attr = attr;
        _type = type;
    }

    public Attribute Attribute => _attr;

    public Type Type => _type;

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
    /// Called by the <see cref="TypeScriptGroupOrPackageAttributeImpl.ConfigureResDescriptor(IActivityMonitor, TypeScriptContext, ResSpaceConfiguration)"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="tsPackage">The package attribute.</param>
    /// <param name="context">The TypeScript context.</param>
    /// <param name="d">The package to configure.</param>
    /// <param name="resourcesConfiguration">The package's configuration.</param>
    /// <returns>Must return true on success, false on error (errors must be logged).</returns>
    protected internal abstract bool OnConfiguredDescriptor( IActivityMonitor monitor,
                                                          TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                          TypeScriptContext context,
                                                          ResPackageDescriptor d,
                                                          ResSpaceConfiguration resourcesConfiguration );
}
