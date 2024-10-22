using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements <see cref="RoutedComponentAttribute"/>.
/// </summary>
public class RoutedComponentAttributeImpl : TypeScriptPackageAttributeImpl, ITSCodeGeneratorFactory, ITSCodeGenerator
{
    /// <summary>
    /// Initializes a new <see cref="RoutedComponentAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public RoutedComponentAttributeImpl( IActivityMonitor monitor, RoutedComponentAttribute attr, Type type )
        : base( monitor, attr, type )
    {
        if( !typeof( RoutedComponent ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[RoutedComponent] can only decorate a RoutedComponent: '{type:N}' is not a RoutedComponent." );
        }
        if( !typeof( RoutedComponent ).IsAssignableFrom( attr.TargetRoutedComponent )
            && attr.TargetRoutedComponent != typeof( CKGenAppModule ) )
        {
            monitor.Error( $"[RoutedComponent] on '{type:C}': TargetRoutedComponent = typeof({attr.TargetRoutedComponent:C}) is not a RoutedComponent (nor the CKGenAppModule)." );
        }
    }

    ITSCodeGenerator? ITSCodeGeneratorFactory.CreateTypeScriptGenerator( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        return this;
    }

    bool ITSCodeGenerator.OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

    bool ITSCodeGenerator.OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

    bool ITSCodeGenerator.StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
    {
        var angular = context.GetAngularContext();
        if( angular != null )
        {
        }
        return true;
    }

}
