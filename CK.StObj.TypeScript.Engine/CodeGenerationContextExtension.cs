using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Setup
{
    public static class CodeGenerationContextExtension
    {
        public static TypeScriptGenerator? GetTypeScriptGenerator( this ICodeGenerationContext @this, IActivityMonitor monitor )
        {
            var binPath = @this.CurrentRun;
            return binPath.Memory.GetCachedInstance<TypeScriptGenerator>( () =>
            {
                var aspect = @this.GlobalServiceContainer.GetService<TypeScriptAspect>( false );
                if( aspect == null )
                {
                    monitor.Warn( $"Skipped TypeScript generation since TypeScript aspect is not configured." );
                    return null;
                }
                var context = aspect.GetTypeScriptCodeGenerationContext( monitor, binPath );
                if( context == null ) return null;

                return new TypeScriptGenerator( context, binPath );
            } );
        }
    }
}
