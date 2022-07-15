using CK.Core;
using CK.TypeScript.CodeGen;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace CK.Setup
{
    /// <summary>
    /// Aspect that drives TypeScript code generation. Handles (and initialized by one) <see cref="TypeScriptAspectConfiguration"/>.
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
            _basePath = context.StObjEngineConfiguration.Configuration.BasePath;
            return true;
        }

        bool IStObjEngineAspect.OnSkippedRun( IActivityMonitor monitor )
        {
            return true;
        }

        bool IStObjEngineAspect.RunPreCode( IActivityMonitor monitor, IStObjEngineRunContext context )
        {
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
                    var jsonCodeGen = genBinPath.CurrentRun.ServiceContainer.GetService<Json.JsonSerializationCodeGen>();
                    if( jsonCodeGen == null )
                    {
                        monitor.Info( $"No Json serialization available in this context." );
                    }
                    var g = new TypeScriptContext( tsContext, genBinPath, jsonCodeGen );
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
            var pathsAndConfig = binPath.ConfigurationGroup.SimilarConfigurations.Select( c => c.GetAspectConfiguration<TypeScriptAspect>() )
                            .Where( c => c != null )
                            .Select( c => (Config: c!, Paths: c!.Elements( "OutputPath" ).Select( p => p?.Value )
                                                                .Where( p => !String.IsNullOrWhiteSpace( p ) )
                                                                .Select( p => MakeAbsolute( _basePath, p ) )
                                                                .Where( p => !p.IsEmptyPath )
                                                                .Distinct()) )
                            .Where( p => p.Paths.Any() )
                            // Reverts the tuple: path => its config (potentially shared with other paths).
                            .SelectMany( p => p.Paths.Select( onePath => (Path: onePath, p.Config) ) )
                            .ToArray();
            if( pathsAndConfig.Length == 0 )
            {
                if( binPath.ConfigurationGroup.SimilarConfigurations.Count != 0 )
                {
                    monitor.Warn( $"Skipped TypeScript generation for BinPathConfiguration {binPath.ConfigurationGroup.Names}: <TypeScript><OutputPath>...</OutputPath></TypeScript> element not found or empty." );
                }
                g = null;
            }
            else
            {
                g = new TypeScriptRoot( pathsAndConfig, _config.PascalCase, _config.GenerateDocumentation, _config.GeneratePocoInterfaces );
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
                            success &= g.Root.Save( monitor );
                        }
                    }
                }
            }
            return success;
        }

    }
}
