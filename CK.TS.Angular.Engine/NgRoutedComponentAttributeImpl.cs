using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
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
        if( !typeof( NgRoutedComponent ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[NgRoutedComponent] can only decorate a NgRoutedComponent: '{type:N}' is not a NgRoutedComponent." );
        }
        if( !typeof( NgRoutedComponent ).IsAssignableFrom( attr.TargetComponent )
            && attr.TargetComponent != typeof( CKGenAppModule ) )
        {
            monitor.Error( $"[NgRoutedComponent] on '{type:C}': TargetRoutedComponent = typeof({attr.TargetComponent:C}) is not a NgRoutedComponent (nor the CKGenAppModule)." );
        }
        if( attr.HasRoutes && attr.Route == "" )
        {
            monitor.Error( $"[NgRoutedComponent] on '{type:C}': HasRoutes = true and Route = \"\" combination is invalid." );
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

    protected override void OnConfigure( IActivityMonitor monitor, IStObjMutableItem o )
    {
        if( Attribute.TargetComponent != typeof( CKGenAppModule ) )
        {
            o.Requires.AddNew( Attribute.TargetComponent, StObjRequirementBehavior.ErrorIfNotStObj );
        }
    }

}
