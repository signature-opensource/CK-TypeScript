using CK.Core;
using CK.Setup;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace CK.StObj.TypeScript.Engine;

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
    readonly TypeScriptAspect _aspect;

    public TypeScriptPackageAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type, TypeScriptAspect aspect )
    {
        _attr = attr;
        _type = type;
        _aspect = aspect;
        if( !typeof( TypeScriptPackage ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[TypeScriptPackage] can only be set on a TypeScriptPackage: '{type:N}' is not a TypeScriptPackage." );
        }
    }

    public virtual void Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        //var transformers = owner.GetTypeCustomAttributes<TypeScriptTransformerAttributeImpl>().ToList();
        //_aspect.RegisterTransfomers( _type, transformers );
    }

    public virtual void Configure( IActivityMonitor monitor, IStObjMutableItem o )
    {
        if( _attr.Package != null )
        {
            if( o.Container.Type == null ) o.Container.Type = _attr.Package;
            else if( o.Container.Type != _attr.Package )
            {
                monitor.Error( $"{o.ToString()}: [TypeScriptPackageAttribute] sets Package to be '{_attr.Package.Name}' but it is already '{o.Container.Type:N}'." );
            }
        }
    }


}
