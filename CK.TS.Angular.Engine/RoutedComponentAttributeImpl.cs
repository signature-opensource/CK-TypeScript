using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using System;

namespace CK.TS.Angular.Engine;

public class RoutedComponentAttributeImpl : TypeScriptPackageAttributeImpl
{
    public RoutedComponentAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type, TypeScriptAspect aspect )
        : base( monitor, attr, type, aspect )
    {
        if( !typeof( RoutedComponent ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[RoutedComponent] can only decorate a RoutedComponent: '{type:N}' is not a RoutedComponent." );
        }
    }
}
