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
public abstract class TypeScriptPackageAttributeImplExtension : IAttributeContextBoundInitializer
{
    readonly Attribute _attr;
    readonly Type _type;

    protected TypeScriptPackageAttributeImplExtension( Attribute attr, Type type )
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
        var tsPackage = owner.GetTypeCustomAttributes<TypeScriptPackageAttributeImpl>().SingleOrDefault();
        if( tsPackage == null )
        {
            monitor.Error( $"[{AttributeName}] on '{owner.Type:N}' requires the [TypeScriptPackage] (or a specialization) to also be declared." );
        }
        else
        {
            tsPackage.AddExtension( this );
            OnInitialize(  monitor, tsPackage, owner );
        }
    }

    /// <summary>
    /// Enables initialization for this extension.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="tsPackage">The associated package.</param>
    /// <param name="owner">The decorated type.</param>
    protected abstract void OnInitialize( IActivityMonitor monitor, TypeScriptPackageAttributeImpl tsPackage, ITypeAttributesCache owner );

    /// <summary>
    /// Called by the <see cref="TypeScriptPackageAttributeImpl.GenerateCode(IActivityMonitor, TypeScriptContext)"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="tsPackage">The associated package.</param>
    /// <param name="context">The TypeScript context.</param>
    /// <returns>Must return true on success, false on error (errors must be logged).</returns>
    protected internal abstract bool GenerateCode( IActivityMonitor monitor, TypeScriptPackageAttributeImpl tsPackage, TypeScriptContext context );
}
