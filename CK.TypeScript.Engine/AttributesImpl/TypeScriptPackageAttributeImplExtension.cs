using CK.Core;
using CK.Setup;
using System;
using System.Linq;
using System.Reflection;

namespace CK.TypeScript.Engine;

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

    protected abstract void OnInitialize( IActivityMonitor monitor, TypeScriptPackageAttributeImpl tsPackage, ITypeAttributesCache owner );

    protected internal abstract bool GenerateCode( IActivityMonitor monitor, TypeScriptPackageAttributeImpl tsPackage, TypeScriptContext context );
}
