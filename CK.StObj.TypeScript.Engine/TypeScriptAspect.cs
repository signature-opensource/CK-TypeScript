using CK.Core;
using CK.Setup.PocoJson;
using CK.TypeScript.CodeGen;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Aspect that drives TypeScript code generation. Handles (and initialized by one) <see cref="TypeScriptAspectConfiguration"/>.
    /// </summary>
    public class TypeScriptAspect : IStObjEngineAspect
    {
        readonly TypeScriptAspectConfiguration _tsConfig;
        NormalizedPath _basePath;
        readonly List<TypeScriptContext> _generators;

        /// <summary>
        /// Initializes a new aspect from its configuration.
        /// </summary>
        /// <param name="config">The aspect configuration.</param>
        public TypeScriptAspect( TypeScriptAspectConfiguration config )
        {
            _tsConfig = config;
            _generators = new List<TypeScriptContext>();
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
            foreach( var genBinPath in context.AllBinPaths )
            {
                // Skip the purely unified BinPath.
                if( genBinPath.CurrentRun.ConfigurationGroup.IsUnifiedPure ) continue;
                // Obtains the TypeScriptAspectConfiguration for all the BinPaths of the ConfigurationGroup.
                // We MAY here decide that ONE BinPath have more than one TypeScriptAspectConfiguration, but
                // currently, one BinPath can define 0 or 1 TypeScriptAspectConfiguration.
                var rootedConfigs = GetRootedConfigurations( monitor, genBinPath );
                if( rootedConfigs != null )
                {
                    var pocoTypeSystem = genBinPath.CurrentRun.ServiceContainer.GetRequiredService<IPocoTypeSystem>();
                    var jsonCodeGen = genBinPath.CurrentRun.ServiceContainer.GetService<IPocoJsonGeneratorService>();
                    if( jsonCodeGen?.JsonNames == null )
                    {
                        monitor.Info( $"No Json serialization available in this context." );
                    }
                    foreach( var tsBinPathconfig in rootedConfigs )
                    {
                        var g = new TypeScriptContext( genBinPath, _tsConfig, tsBinPathconfig, pocoTypeSystem, jsonCodeGen?.JsonNames );
                        _generators.Add( g );
                        if( !g.Run( monitor ) )
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        IReadOnlyCollection<TypeScriptAspectBinPathConfiguration>? GetRootedConfigurations( IActivityMonitor monitor,
                                                                                            ICodeGenerationContext genBinPath )
        {
            if( !_basePath.IsRooted ) Throw.InvalidOperationException( $"Configuration BasePath '{_basePath}' must be rooted." );

            static NormalizedPath MakeAbsoluteAndNormalize( NormalizedPath basePath, NormalizedPath p )
            {
                if( p.LastPart.Equals( "ck-gen", StringComparison.OrdinalIgnoreCase ) )
                {
                    p = p.RemoveLastPart();
                }
                if( !p.IsRooted )
                {
                    p = basePath.Combine( p );
                }
                return p.ResolveDots();
            }

            var binPath = genBinPath.CurrentRun;
            var rootedConfigs = binPath.ConfigurationGroup.SimilarConfigurations
                            .Select( c => c.GetAspectConfiguration<TypeScriptAspect>() )
                            .Where( c => c != null )
                            .Select( c => (Path: c!.Attribute( TypeScriptAspectConfiguration.xTargetProjectPath )?.Value, c!) )
                            .Where( c => !string.IsNullOrWhiteSpace( c.Path ) )
                            .Select( c => (Path: MakeAbsoluteAndNormalize( _basePath, c.Path ), c.Item2) )
                            .Where( c => !c.Path.IsEmptyPath )
                            .Select( c => new TypeScriptAspectBinPathConfiguration( c.Item2 ) { TargetProjectPath = c.Path } )
                            .ToList();
            if( rootedConfigs.Count == 0 )
            {
                if( binPath.ConfigurationGroup.SimilarConfigurations.Count != 0 )
                {
                    monitor.Warn( $"Skipped TypeScript generation for BinPathConfiguration {binPath.ConfigurationGroup.Names}: " +
                                  $"no <TypeScript TargetProjectPath=\"...\"></TypeScript> element found or empty TargetProjectPath." );
                }
                return null;
            }
            return rootedConfigs;
        }

        bool IStObjEngineAspect.Terminate( IActivityMonitor monitor, IStObjEngineTerminateContext context )
        {
            bool success = true;
            if( context.EngineStatus.Success )
            {
                foreach( var g in _generators )
                {
                    success &= g.Save( monitor );
                }
            }
            return success;
        }

    }
}
