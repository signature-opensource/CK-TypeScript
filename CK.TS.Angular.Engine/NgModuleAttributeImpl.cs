using CK.Core;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using System;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements <see cref="NgModuleAttribute"/>.
/// </summary>
public class NgModuleAttributeImpl : TypeScriptPackageAttributeImpl
{
    /// <summary>
    /// Initializes a new <see cref="NgModuleAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public NgModuleAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type )
        : base( monitor, attr, type )
    {
        if( !typeof( NgModule ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[NgModule] can only decorate a NgModule: '{type:N}' is not a NgModule." );
        }
    }
}
