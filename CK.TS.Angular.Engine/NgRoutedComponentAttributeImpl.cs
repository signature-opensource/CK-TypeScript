using CK.Core;
using CK.Setup;
using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements <see cref="NgRoutedComponentAttribute"/>.
/// </summary>
public class NgRoutedComponentAttributeImpl : NgComponentAttributeImpl
{
    /// <summary>
    /// Initializes a new <see cref="NgRoutedComponentAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public NgRoutedComponentAttributeImpl( IActivityMonitor monitor, NgRoutedComponentAttribute attr, Type type )
        : base( monitor, attr, type )
    {
        if( !typeof( INgRoutedComponent ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[NgRoutedComponent] can only decorate a NgRoutedComponent: '{type:N}' is not a NgRoutedComponent." );
        }
        if( !typeof( INgComponent ).IsAssignableFrom( attr.TargetComponent ) )
        {
            monitor.Error( $"[NgRoutedComponent] on '{type:C}': TargetRoutedComponent = typeof({attr.TargetComponent:C}) is not a NgComponent." );
        }
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public new NgRoutedComponentAttribute Attribute => Unsafe.As<NgRoutedComponentAttribute>( base.Attribute );

    /// <summary>
    /// Gets the route to register under the <see cref="NgRoutedComponentAttribute.TargetComponent"/>.
    /// </summary>
    public string Route => Attribute.Route ?? FileComponentName;

    protected override bool OnCreateResPackageDescriptor( IActivityMonitor monitor,
                                                          TypeScriptContext context,
                                                          ResSpaceConfiguration spaceBuilder,
                                                          ResPackageDescriptor d )
    {
        if( Attribute.TargetComponent != typeof( AppComponent ) )
        {
            d.Requires.Add( Attribute.TargetComponent );
        }
        return base.OnCreateResPackageDescriptor( monitor, context, spaceBuilder, d );
    }
}
