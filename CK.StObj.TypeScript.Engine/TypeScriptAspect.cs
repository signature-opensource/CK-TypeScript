using CK.Core;
using CK.Text;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace CK.Setup
{
    /// <summary>
    /// Aspect that drives TypeScript code generation. Implements <see cref="TypeScriptAspectConfiguration"/>.
    /// </summary>
    public class TypeScriptAspect : IStObjEngineAspect
    {
        readonly TypeScriptAspectConfiguration _config;
        NormalizedPath _basePath;
        TypeScriptContext?[] _generators;

        /// <summary>
        /// Initializes a new aspect from its configuration.
        /// </summary>
        /// <param name="config">The aspect configuration.</param>
        public TypeScriptAspect( TypeScriptAspectConfiguration config )
        {
            _config = config;
        }

        bool IStObjEngineAspect.Configure( IActivityMonitor monitor, IStObjEngineConfigureContext context )
        {
            _basePath = context.StObjEngineConfiguration.BasePath;
            return true;
        }

        bool IStObjEngineAspect.RunPostCode( IActivityMonitor monitor, IStObjEnginePostCodeRunContext context )
        {
            _generators = new TypeScriptContext?[context.AllBinPaths.Count];

            int idx = 0;
            foreach( var genBinPath in context.AllBinPaths )
            {
                TypeScriptRoot? tsContext = CreateGenerationContext( monitor, genBinPath );
                if( tsContext != null )
                {
                    var g = new TypeScriptContext( tsContext, genBinPath );
                    _generators[idx++] = g;
                    if( !g.Run( monitor ) )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        TypeScriptRoot? CreateGenerationContext( IActivityMonitor monitor, ICodeGenerationContext genBinPath )
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
            TypeScriptRoot? g;
            var binPath = genBinPath.CurrentRun;
            var paths = binPath.BinPathConfigurations.Select( c => c.GetAspectConfiguration<TypeScriptAspect>() )
                            .Where( c => c != null )
                            .SelectMany( c => c!.Elements( "OutputPath" ) )
                            .Select( e => e.Value )
                            .Where( p => !String.IsNullOrWhiteSpace( p ) )
                            .Select( p => MakeAbsolute( _basePath, p ) )
                            .Where( p => !p.IsEmptyPath );
            if( !paths.Any() )
            {
                if( binPath.BinPathConfigurations.Count != 0 )
                {
                    monitor.Warn( $"Skipped TypeScript generation for BinPathConfiguration {binPath.Names}: <TypeScript><OutputPath>...</OutputPath></TypeScript> element not found or empty." );
                }
                g = null;
            }
            else
            {
                g = new TypeScriptRoot( paths, _config.PascalCase, _config.GenerateDocumentation );
            }

            return g;
        }

        bool IStObjEngineAspect.Terminate( IActivityMonitor monitor, IStObjEngineTerminateContext context )
        {
            bool success = true;
            if( context.EngineStatus.Success )
            {
                using( monitor.OpenInfo( $"Saving TypeScript files." ) )
                {
                    foreach( var g in _generators )
                    {
                        if( g != null )
                        {
                            success &= g.Root.Root.Save( monitor, g.Root.OutputPaths );
                        }
                    }
                }
            }
            return success;
        }


    }
}
