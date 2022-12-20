using CK.Core;
using CK.Setup.PocoJson;
using CK.TypeScript.CodeGen;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

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
                var outputPaths = GetOutputPaths( monitor, genBinPath );
                if( outputPaths != null )
                {
                    var pocoTypeSystem = genBinPath.CurrentRun.ServiceContainer.GetRequiredService<IPocoTypeSystem>();
                    var jsonCodeGen = genBinPath.CurrentRun.ServiceContainer.GetService<IPocoJsonGeneratorService>();
                    if( jsonCodeGen?.JsonNames == null )
                    {
                        monitor.Info( $"No Json serialization available in this context." );
                    }
                    var g = new TypeScriptContext( outputPaths, genBinPath, _config, pocoTypeSystem, jsonCodeGen?.JsonNames );
                    _generators[idx++] = g;
                    if( !g.Run( monitor ) )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        IReadOnlyCollection<(NormalizedPath Path, XElement Config)>? GetOutputPaths( IActivityMonitor monitor, ICodeGenerationContext genBinPath )
        {
            static NormalizedPath MakeAbsolute( NormalizedPath basePath, NormalizedPath p )
            {
                Throw.CheckArgument( "Configuration BasePath must not be empty.", !basePath.IsEmptyPath );
                if( !basePath.IsRooted ) Throw.InvalidOperationException( $"Configuration BasePath '{basePath}' must be rooted." );
                if( !p.IsRooted )
                {
                    p = basePath.Combine( p );
                }
                return p.ResolveDots();
            }
            TypeScriptRoot? g;
            var binPath = genBinPath.CurrentRun;
            var pathsAndConfig = binPath.ConfigurationGroup.SimilarConfigurations
                            .Select( c => c.GetAspectConfiguration<TypeScriptAspect>() )
                            .Where( c => c != null )
                            .Select( c => (Path: c!.Attribute( "OutputPath" )?.Value ?? c.Element( "OutputPath" )?.Value, Config: c!) )
                            .Where( c => !string.IsNullOrWhiteSpace( c.Path ) )
                            .Select( c => (Path: MakeAbsolute( _basePath, c.Path ), c.Config) )
                            .Where( c => !c.Path.IsEmptyPath )
                            .ToArray();
            if( pathsAndConfig.Length == 0 )
            {
                if( binPath.ConfigurationGroup.SimilarConfigurations.Count != 0 )
                {
                    monitor.Warn( $"Skipped TypeScript generation for BinPathConfiguration {binPath.ConfigurationGroup.Names}: <TypeScript><OutputPath>...</OutputPath></TypeScript> element not found or empty." );
                }
                return null;
            }
            return pathsAndConfig;
        }

        bool IStObjEngineAspect.Terminate( IActivityMonitor monitor, IStObjEngineTerminateContext context )
        {
            bool success = true;
            if( context.EngineStatus.Success )
            {
                using( monitor.OpenInfo( $"Saving generated TypeScript files..." ) )
                {
                    foreach( var g in _generators )
                    {
                        if( g != null )
                        {
                            success &= g.Root.SaveTS( monitor );
                        }
                    }
                }

                if( !success ) return false;

                if( _config.SkipTypeScriptBuild )
                {
                    monitor.Info( "Skipping TypeScript build." );
                }
                else
                {
                    using( monitor.OpenInfo( $"Starting TypeScript build..." ) )
                    {
                        foreach( var g in _generators )
                        {
                            if( g != null )
                            {
                                success &= YarnPackageGenerator.SaveBuildConfig( monitor, g.Root )
                                            & YarnPackageGenerator.RunNodeBuild( monitor, g.Root );
                            }
                        }
                    }
                }
            }
            return success;
        }

    }
}
