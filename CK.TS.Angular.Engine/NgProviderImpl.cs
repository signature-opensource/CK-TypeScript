using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript;
using CK.TypeScript.CodeGen;
using System;

namespace CK.TS.Angular.Engine;


public sealed class NgProviderAttributeImpl : IAttributeContextBound, ITSCodeGeneratorFactory, ITSCodeGenerator
{
    /// <summary>
    /// Initializes a new <see cref="NgProviderAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public NgProviderAttributeImpl( IActivityMonitor monitor, NgProviderAttribute attr, Type type )
    {
        if( !typeof( NgProvider ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[NgProvider] can only decorate a NgProvider: '{type:N}' is not a NgProvider." );
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
        var angular = context.GetAngularCodeGen();
        return true;
    }

}
