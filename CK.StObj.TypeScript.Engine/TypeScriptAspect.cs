using CK.Core;
using CK.TypeScript.CodeGen;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
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
                // currently, on BinPath can define 0 or 1 TypeScriptAspectConfiguration.
                var rootedConfigs = GetRootedConfigurations( monitor, genBinPath );
                if( rootedConfigs != null )
                {
                    var jsonCodeGen = genBinPath.CurrentRun.ServiceContainer.GetService<Json.JsonSerializationCodeGen>();
                    if( jsonCodeGen == null )
                    {
                        monitor.Info( $"No Json serialization available in this context." );
                    }
                    foreach( var tsBinPathconfig in rootedConfigs )
                    {
                        var g = new TypeScriptContext( genBinPath, _tsConfig, tsBinPathconfig, jsonCodeGen );
                        _generators.Add( g );
                        if( !g.Run( monitor ) )
                        {
                            return false;
                        }
                    }
                    // The CK.Setup.TypeScriptCrisCommandGeneratorImpl in CK.Cris.AspNet.Engine
                    // (triggered by the static CK.Cris.TypeScript.TypeScriptCrisCommandGenerator) needs
                    // hooks the Poco TypeScript generation to handle Poco that are commands.
                    // Only CK.Cris.IAbstractCommand thar are declared (by [TypeScript] or by the configuration)
                    // are generated (this why, this uses a simple hook).
                    // But CK.Cris.AspNet.CrisAspNetService needs to know tne TypeScriptified commands: they are
                    // the only ones that must be handled by the /.cris handler. The allow-list of the AspNet endpoint
                    // IS the same as the "TypeScript enabled commands" (we don't want a second filter to be configured
                    // by the developer for this).
                    // TODO:
                    // - Decide:
                    //      - If a central modeling of "AllowedEndpoints" can exist on the CrisPocoModel. This may
                    //        be a simple list of endpoint names that can be used receive the command.
                    //        This would be a cool feature (discoverability, transparency).
                    //      - Or the allow-list should be managed by each endpoint, without any central model.
                    //        This is less tempting... But may be simpler.
                    // 
                    //      In the 1) it will be up to the CK.Cris.AspNet.Engine to alter/mutate the PocoCrisModel...
                    //      AFTER the code generation step :(
                    // 
                    //      In the 2) the "default AspNet Cris endpoint" has a special status and can do what it can
                    //      to impact/configure the CrisAspNetEngine... But here again, this happens after the code
                    //      generation step.
                    //
                    // It seems that we need to move the TypeScriptContext initialization up in the process to have the
                    // list of declare TSTypes earlier... (its Run() can stay in the PostCode step).
                    //
                    //
                    // This seems doable: if this aspect implements ICSCodeGenerator, its
                    // Implement(IActivityMonitor, ICSCodeGenerationContext) will be called. It will have to "trampoline"
                    // once to give the opportunity to the jsonCodeGen to be available in the CurrentRun.ServiceContainer.
                    // It then can initialize the TypeScriptContexts for the BinPaths.
                    // BUT the discovery is done in the Run() (as of today): it seems that nearly everything except the very
                    // last step must be moved to the Implement step.
                    //
                    // Once done, the Implement step will be able to generate C# code with the "default aspnet endpoint"
                    // configuration of the allowed commands (how this will be done is another story...).
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
