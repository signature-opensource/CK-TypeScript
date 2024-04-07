using CK.Core;
using CK.Setup.PocoJson;
using CK.TypeScript.CodeGen;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Aspect that drives TypeScript code generation. Handles (and initialized) by the <see cref="TypeScriptAspectConfiguration"/>.
    /// </summary>
    public class TypeScriptAspect : IStObjEngineAspect, ICSCodeGenerator
    {
        readonly TypeScriptAspectConfiguration _tsConfig;
        readonly List<TypeScriptContext>? _deferedSave;

        /// <summary>
        /// Initializes a new aspect from its configuration.
        /// </summary>
        /// <param name="config">The aspect configuration.</param>
        public TypeScriptAspect( TypeScriptAspectConfiguration config )
        {
            _tsConfig = config;
            _deferedSave = config.DeferFileSave ? new List<TypeScriptContext>() : null;
        }

        bool IStObjEngineAspect.Configure( IActivityMonitor monitor, IStObjEngineConfigureContext context ) => true;

        bool IStObjEngineAspect.OnSkippedRun( IActivityMonitor monitor ) => true;

        bool IStObjEngineAspect.RunPreCode( IActivityMonitor monitor, IStObjEngineRunContext context ) => true;

        CSCodeGenerationResult ICSCodeGenerator.Implement( IActivityMonitor monitor, ICSCodeGenerationContext c )
        {
            // Skips the purely unified BinPath.
            if( c.CurrentRun.ConfigurationGroup.IsUnifiedPure ) return CSCodeGenerationResult.Success;

            // We must not only wait for the IPocoTypeSystem to be available but we also need to know if the optional IPocoJsonSerializationServiceEngine
            // is available or not... The IPocoJsonSerializationServiceEngine requires first IPocoSerializationServiceEngine to be available.
            // To know if the Json serialization will eventually be available, it COULD be as easy as:
            //
            // bool isJsonHere = Type.GetType( "CK.Core.CommonPocoJsonSupport, CK.Poco.Exc.Json" ) != null;
            //
            // Unfortunately, in a test contexts (where all objects are not registered), this will wait indefinitely. We must use
            // a better check by exploiting the VFeatures: these are the assemblies for which at least one CK type has been handled.
            //
            Throw.DebugAssert( typeof( CommonPocoJsonSupport ).Assembly.GetName().Name == "CK.Poco.Exc.Json" );
            bool isJsonHere = c.CurrentRun.EngineMap.Features.Any( f => f.Name == "CK.Poco.Exc.Json" );
            return new CSCodeGenerationResult( isJsonHere ? nameof( WaitForJsonSerialization ) : nameof( WaitForLockedTypeSystem ) );
        }

        CSCodeGenerationResult WaitForLockedTypeSystem( IActivityMonitor monitor, ICSCodeGenerationContext c, IPocoTypeSystemBuilder typeSystemBuilder )
        {
            if( !typeSystemBuilder.IsLocked )
            {
                return new CSCodeGenerationResult( nameof( WaitForLockedTypeSystem ) );
            }
            // Gets the type system by locking again the builder.
            IPocoTypeSystem typeSystem = typeSystemBuilder.Lock( monitor );

            using( monitor.OpenInfo( $"PocoTypeSystemBuilder is locked (without Json serialization): handling TypeScript generation." ) )
            {
                return Run( monitor, c, typeSystem, null )
                        ? CSCodeGenerationResult.Success
                        : CSCodeGenerationResult.Failed;
            }
        }

        CSCodeGenerationResult WaitForJsonSerialization( IActivityMonitor monitor, ICSCodeGenerationContext c )
        {
            var jsonSerialization = c.CurrentRun.ServiceContainer.GetService<IPocoJsonSerializationServiceEngine>();
            if( jsonSerialization == null )
            {
                return new CSCodeGenerationResult( nameof( WaitForJsonSerialization ) );
            }

            using( monitor.OpenInfo( $"IPocoJsonSerializationServiceEngine is available: handling TypeScript generation." ) )
            {
                return Run( monitor, c, jsonSerialization.SerializableLayer.TypeSystem, jsonSerialization )
                        ? CSCodeGenerationResult.Success
                        : CSCodeGenerationResult.Failed;
            }
        }

        bool Run( IActivityMonitor monitor, ICSCodeGenerationContext codeContext, IPocoTypeSystem typeSystem, IPocoJsonSerializationServiceEngine? jsonSerialization )
        {
            var binPath = codeContext.CurrentRun; 
            // Obtains all the TypeScriptAspectConfiguration for all the BinPaths of the ConfigurationGroup.
            // One BinPath can have any number of TypeScriptAspectConfiguration. The TypeFilterName identifies
            // one configuration among the others in the same BinPath.
            if( !GetRootedConfigurations( monitor, binPath, out var rootedConfigs ) )
            {
                return false;
            }
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
                                                               binPath,
                                                               _tsConfig,
                                                               tsBinPathconfig,
                                                               typeSystem.SetManager.AllExchangeable,
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
                    if( initializer.TypeScriptExchangeableSet.SameContentAs( typeSystem.SetManager.AllExchangeable ) )
                    {
                        exchangeableNames = jsonSerialization.SerializableLayer.SerializableNames;
                    }
                    else
                    {
                        // Uses the Clone virtual method to handle future evolution that may not use
                        // the standard PocoTypeNameMap implementation for Json names.
                        exchangeableNames = jsonSerialization.SerializableLayer.SerializableNames.Clone( initializer.TypeScriptExchangeableSet );
                    }
                    // Regardless of whether it is the same as AllExchangeable or a sub set, we register the ExhangeableRuntimeTypeFilter.
                    jsonSerialization.SerializableLayer.RegisterExchangeableRuntimeFilter( monitor, tsBinPathconfig.TypeFilterName, initializer.TypeScriptExchangeableSet );
                }
                // The TypeScriptContext for this configuration can now be initialized and run.
                var g = new TypeScriptContext( codeContext, _tsConfig, tsBinPathconfig, initializer, exchangeableNames );
                if( !g.Run( monitor ) )
                {
                    return false;
                }
                // Save or defer.
                if( _deferedSave != null ) _deferedSave.Add( g );
                else if( !g.Save( monitor ) ) return false;
            }
            return true;
        }


        bool IStObjEngineAspect.RunPostCode( IActivityMonitor monitor, IStObjEnginePostCodeRunContext context ) => true;

        bool GetRootedConfigurations( IActivityMonitor monitor,
                                      IGeneratedBinPath binPath,
                                      [NotNullWhen(true)]out IReadOnlyCollection<TypeScriptAspectBinPathConfiguration>? configurations )
        {
            var basePath = binPath.ConfigurationGroup.EngineConfiguration.BasePath;
            if( !basePath.IsRooted ) Throw.InvalidOperationException( $"Configuration BasePath '{basePath}' must be rooted." );

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

            var configurationNames = BinPathConfiguration.GetAspectConfigurationNames( nameof( TypeScriptAspectConfiguration ) );
            configurations = binPath.ConfigurationGroup.SimilarConfigurations
                            .SelectMany( c => c.AspectConfigurations.Where( e => configurationNames.Contains( e.Name.LocalName ) ) )
                            .Select( c => (Path: c!.Attribute( TypeScriptAspectConfiguration.xTargetProjectPath )?.Value, c!) )
                            .Where( c => !string.IsNullOrWhiteSpace( c.Path ) )
                            .Select( c => (Path: MakeAbsoluteAndNormalize( basePath, c.Path ), c.Item2) )
                            .Where( c => !c.Path.IsEmptyPath )
                            .Select( c => new TypeScriptAspectBinPathConfiguration( c.Item2 ) { TargetProjectPath = c.Path } )
                            .ToList();
            if( configurations.Count == 0 )
            {
                monitor.Warn( $"Skipped TypeScript generation for BinPathConfiguration '{binPath.ConfigurationGroup.Names}': " +
                              "no <TypeScript TargetProjectPath=\"...\"></TypeScript> element found or empty TargetProjectPath." );
                return true;
            }
            // This test is not perfect: the TargetProjectPath should be unique among all the TypeScript of all the GeneratedBinPath.
            // Here we check only inside one but this is acceptable.
            var targetPath = configurations.GroupBy( c => c.TargetProjectPath.Path, StringComparer.OrdinalIgnoreCase );
            if( targetPath.Count() != configurations.Count )
            {
                foreach( var g in targetPath.Where( g => g.Count() > 1 ) )
                {
                    monitor.Error( $"TypeScript configuration with TargetProjectPath=\"{g.Key}\" appear more than once in BinPathConfiguration '{binPath.ConfigurationGroup.Names}'. " +
                                   $"Each configuration must target a different output path." );
                    return false;
                }
            }
            // This test is important: the TypeFilterName is registered (as an ExchangeableRuntimeFilter) and each set of types
            // must be clearly identified.
            var filterNames = configurations.GroupBy( c => c.TypeFilterName, StringComparer.OrdinalIgnoreCase );
            if( filterNames.Count() != configurations.Count )
            {
                foreach( var g in filterNames.Where( g => g.Count() > 1 ) )
                {
                    monitor.Error( $"TypeScript configuration with TypeFilterName=\"{g.Key}\" appear more than once in BinPathConfiguration '{binPath.ConfigurationGroup.Names}'. " +
                                   $"They must use different names as they identify different set of types for the serialization layer." );
                    return false;
                }
            }
            return true;
        }

        bool IStObjEngineAspect.Terminate( IActivityMonitor monitor, IStObjEngineTerminateContext context )
        {
            bool success = true;
            if( _deferedSave != null && context.EngineStatus.Success )
            {
                foreach( var g in _deferedSave )
                {
                    success &= g.Save( monitor );
                }
            }
            return success;
        }

    }
}
