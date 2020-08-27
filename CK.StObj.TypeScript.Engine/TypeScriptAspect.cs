using CK.Core;
using CK.Text;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class TypeScriptAspect : IStObjEngineAspect
    {
        readonly TypeScriptAspectConfiguration _config;
        readonly Dictionary<IGeneratedBinPath, TypeScriptCodeGenerationContext?> _contexts;
        NormalizedPath _basePath;

        /// <summary>
        /// Initializes a new aspect from its configuration.
        /// </summary>
        /// <param name="config">The aspect configuration.</param>
        public TypeScriptAspect( TypeScriptAspectConfiguration config )
        {
            _config = config;
            _contexts = new Dictionary<IGeneratedBinPath, TypeScriptCodeGenerationContext?>();
        }

        bool IStObjEngineAspect.Configure( IActivityMonitor monitor, IStObjEngineConfigureContext context )
        {
            _basePath = context.ExternalConfiguration.BasePath;
            return true;
        }

        bool IStObjEngineAspect.RunPostCode( IActivityMonitor monitor, IStObjEngineRunContext context )
        {
            
            int idx = 0;
            foreach( var binPath in context.AllBinPaths )
            {
                var g = GetTypeScriptCodeGenerationContext( monitor, binPath );

                var second = new List<MultiPassCodeGeneration>();
                secondPass[i++] = (g, second);
                if( !g.Result.GenerateSourceCodeFirstPass( _monitor, g, _config.InformationalVersion, second ) )
                {
                    _status.Success = false;
                    break;
                }
            }

            // Calls all ICodeGenerator items.
            foreach( var g in EngineMap.AllTypesAttributesCache.Values.SelectMany( attr => attr.GetAllCustomAttributes<ITSCodeGenerator>() ) )
            {
                var second = MultiPassCodeGeneration.FirstPass( monitor, g, codeGenContext ).SecondPass;
                if( second != null ) collector.Add( second );
            }
        }

        bool IStObjEngineAspect.Terminate( IActivityMonitor monitor, IStObjEngineTerminateContext context )
        {
            bool success = true;
            if( context.EngineStatus.Success )
            {
                using( monitor.OpenInfo( $"Saving TypeScript files." ) )
                {
                    foreach( var kv in _contexts )
                    {
                        if( kv.Value != null && _contexts.TryGetValue( kv.Key, out var tsCodeContext ) && tsCodeContext != null )
                        {
                            success &= tsCodeContext.Root.Save( monitor, tsCodeContext.OutputPaths );
                        }
                    }
                }
            }
            return success;
        }

        /// <summary>
        /// Gets the <see cref="TypeScriptCodeGenerationContext"/> to use for a <see cref="IGeneratedBinPath"/>.
        /// Returns null if no TypeScript generation should be done.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="binPath">The current BinPath.</param>
        /// <returns>The generator to use or null if no TypeScript generation should be done.</returns>
        public TypeScriptCodeGenerationContext? GetTypeScriptCodeGenerationContext( IActivityMonitor monitor, IGeneratedBinPath binPath )
        {
            static NormalizedPath MakeAbsolute( NormalizedPath basePath, NormalizedPath p )
            {
                if( basePath.IsEmptyPath ) throw new InvalidOperationException( "Configuration BasePath is empty." );
                if( !basePath.IsRooted ) throw new InvalidOperationException( $"Configuration BasePath '{basePath}' is not rooted." );
                if( !p.IsRooted )
                {
                    p = basePath.Combine( p );
                }
                return p.ResolveDots();
            }

            if( !_contexts.TryGetValue( binPath, out var context ) )
            {
                var paths = binPath.BinPathConfigurations.Select( c => c.GetAspectConfiguration<TypeScriptAspect>()?.Element( "OutputPath" )?.Value )
                                             .Where( p => !String.IsNullOrWhiteSpace( p ) )
                                             .Select( p => MakeAbsolute( _basePath, p ) )
                                             .Where( p => !p.IsEmptyPath );
                if( !paths.Any() )
                {

                    monitor.Warn( $"Skipped TypeScript generation for BinPathConfiguration {binPath.Names}: <TypeScript><OutputPath>...</OutputPath></TypeScript> element not found or empty." );
                    context = null;
                }
                else
                {
                    context = new TypeScriptCodeGenerationContext( paths, _config.PascalCase );
                }
                _contexts.Add( binPath, context );
            }
            return context;
        }

    }
}
