using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using System;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements <see cref="RoutedComponentAttribute"/>.
/// </summary>
public class RoutedComponentAttributeImpl : TypeScriptPackageAttributeImpl
{
    /// <summary>
    /// Initializes a new <see cref="RoutedComponentAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public RoutedComponentAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type )
        : base( monitor, attr, type )
    {
        if( !typeof( RoutedComponent ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[RoutedComponent] can only decorate a RoutedComponent: '{type:N}' is not a RoutedComponent." );
        }
    }
}
