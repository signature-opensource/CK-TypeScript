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
            foreach( var codeGenContext in context.AllBinPaths )
            {
                // Skip the purely unified BinPath.
                if( codeGenContext.CurrentRun.ConfigurationGroup.IsUnifiedPure ) continue;
                // Obtains the TypeScriptAspectConfiguration for all the BinPaths of the ConfigurationGroup.
                // We MAY here decide that ONE BinPath have more than one TypeScriptAspectConfiguration, but
                // currently, one BinPath can define 0 or 1 TypeScriptAspectConfiguration.
                var rootedConfigs = GetRootedConfigurations( monitor, codeGenContext );
                if( rootedConfigs != null )
                {
                    // Tries to obtain the IPocoJsonSerializationServiceEngine (that exposes the IPocoTypeSystem) for this BinPath.
                    // If it is not here (no Json serialization), we obtain the IPocoTypeSystem and we have no exchangeableNames.
                    IPocoTypeSet exchangeableSet;
                    var jsonSerialization = codeGenContext.CurrentRun.ServiceContainer.GetService<IPocoJsonSerializationServiceEngine>();
                    if( jsonSerialization == null )
                    {
                        monitor.Info( $"No Json serialization available in this context." );
                        exchangeableSet = codeGenContext.CurrentRun.ServiceContainer.GetRequiredService<IPocoTypeSystem>().SetManager.AllExchangeable;
                    }
                    else
                    {
                        exchangeableSet = jsonSerialization.SerializableLayer.AllExchangeable;
                    }
                    // We are in a BinPathConfiguration.
                    // Currently, only one TypeScriptAspectBinPathConfiguration is handled in a BinPathConfiguration but this may change.
                    // We loop here on a single item list.
                    // We associate a TypeScriptContext to each TypeScriptAspectBinPathConfiguration and run it.
                    foreach( var tsBinPathconfig in rootedConfigs )
                    {
                        // First handles the configured <Types>, types that have [TypeScript] attribute or are decorated by some
                        // ITSCodeGeneratorType and any type that are decorated with "global" ITSCodeGenerator. Then the discovered globals
                        // ITSCodeGenerator.Initialize are called: new registered types can be added by global generators.
                        // On success, the final TSContextInitializer.TypeScriptExchangeableSet is computed from all the registered types that
                        // are IPocoType from the EmptyExchangeable set: this is an allow list.
                        // => Only Poco compliant types that are reachable from a registered Poco type will be in TypeScriptExchangeableSet
                        //    and handled by the PocoCodeGenerator.
                        var initializer = TSContextInitializer.Create( monitor,
                                                                       codeGenContext,
                                                                       _tsConfig,
                                                                       tsBinPathconfig,
                                                                       exchangeableSet,
                                                                       jsonSerialization );
                        if( initializer == null ) return false;

                        // We now have the Global code generators initialized, the configured attributes on explicitly registered types,
                        // discovered types or newly added types and a set of "TypeScriptExchangeable" Poco types.
                        IPocoTypeNameMap? exchangeableNames = null;
                        if( jsonSerialization != null )
                        {
                            // If Json serialization is available, let's get the name map for them.
                            // It the sets differ, build a dedicated name map for it (Note: this cannot be a subset of the names
                            // beacause of anonymous record names that expose their fields).
                            if( initializer.TypeScriptExchangeableSet.SameContentAs( exchangeableSet ) )
                            {
                                exchangeableNames = jsonSerialization.SerializableLayer.SerializableNames;
                            }
                            else
                            {
                                // Uses the Clone virtual method to handle future evolution that may not use
                                // the standard PocoTypeNameMap implementation for Json names.
                                exchangeableNames = jsonSerialization.SerializableLayer.SerializableNames.Clone( initializer.TypeScriptExchangeableSet );
                            }
                        }
                        // The TypeScriptContext for this configuration can now be initialized and run.
                        var g = new TypeScriptContext( codeGenContext, _tsConfig, tsBinPathconfig, initializer, exchangeableNames );
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
