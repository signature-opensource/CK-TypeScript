using CK.Core;
using CK.Setup.PocoJson;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Setup;

/// <summary>
/// Aspect that drives TypeScript code generation. Handles (and initialized) by the <see cref="TypeScriptAspectConfiguration"/>.
/// </summary>
public partial class TypeScriptAspect : IStObjEngineAspect, ICSCodeGeneratorWithFinalization
{
    // This enables deferring the TypeScript generation at the final step of CS code generation.
    // A first part must run during the CS code generation to be able to register PocoTypeSet.
    // But TypeScript generation itself is not CS code generation and by deferring the TS we allow
    // the participants that subscribed to our TS events to use any Engine services available in the
    // CurrentRun's ServiceContainer.
    // This list is cleared by ICSCodeGenerator.Implement, filled by PrepareRun and its TypeScriptContexts
    // are Run by ICSCodeGeneratorWithFinalization.FinalImplement.
    readonly List<TypeScriptContext> _runContexts;

    // This supports the TypeScriptAspectConfiguration.DeferFileSave: IStObjEngineAspect.Terminate uses it.
    readonly List<TypeScriptContext>? _deferedSave;

    /// <summary>
    /// Initializes a new aspect from its configuration.
    /// </summary>
    /// <param name="config">The aspect configuration.</param>
    public TypeScriptAspect( TypeScriptAspectConfiguration config )
    {
        _deferedSave = config.DeferFileSave ? new List<TypeScriptContext>() : null;
        _runContexts = new List<TypeScriptContext>();
    }

    bool IStObjEngineAspect.Configure( IActivityMonitor monitor, IStObjEngineConfigureContext context )
    {
        var c = context.EngineConfiguration.Configuration;
        var basePath = c.BasePath;
        if( !basePath.IsRooted ) Throw.InvalidOperationException( $"EngineConfiguration.BasePath '{basePath}' must be rooted." );

        var allBinPathConfigurations = c.BinPaths.SelectMany( c => c.FindAspect<TypeScriptBinPathAspectConfiguration>()?.AllConfigurations ?? [] )
                                        .ToList();
        for( int i = 0; i < allBinPathConfigurations.Count; i++ )
        {
            TypeScriptBinPathAspectConfiguration ts = allBinPathConfigurations[i];

            if( !KeepValidTargetProjectPath( monitor, basePath, ts ) )
            {
                allBinPathConfigurations.RemoveAt( i-- );
            }
            else
            {
                // Ugly... Should we implement a NormalizedFolderPath?
                var normalizedBarrels = ts.Barrels.Select( NormalizeFolderPath ).ToList();
                ts.Barrels.Clear();
                ts.Barrels.AddRange( normalizedBarrels );
                // Ensures that the DefaultCulture appears in the ActiveCultures.
                ts.ActiveCultures.Add( ts.DefaultCulture );
            }
        }
        return CheckPathOrTypeScriptSetDuplicate( monitor, allBinPathConfigurations );

        static bool KeepValidTargetProjectPath( IActivityMonitor monitor, NormalizedPath basePath, TypeScriptBinPathAspectConfiguration ts )
        {
            Throw.DebugAssert( ts.Owner != null );
            if( CheckEmptyTargetProjectPath( monitor, ts.Owner, ts ) )
            {
                ts.TargetProjectPath = MakeAbsoluteAndNormalize( basePath, ts.TargetProjectPath );
                if( CheckEmptyTargetProjectPath( monitor, ts.Owner, ts ) )
                {
                    return true;
                }
            }
            return false;

            static bool CheckEmptyTargetProjectPath( IActivityMonitor monitor, BinPathConfiguration owner, TypeScriptBinPathAspectConfiguration ts )
            {
                if( ts.TargetProjectPath.IsEmptyPath || string.IsNullOrWhiteSpace( ts.TargetProjectPath ) )
                {
                    if( ts.Name != null && !owner.ProjectPath.IsEmptyPath )
                    {
                        // A named configuration: its target project is "{ProjectPath}/Clients/{Name}".
                        ts.TargetProjectPath = owner.ProjectPath.AppendPart( "Clients" ).AppendPart( ts.Name );
                        monitor.Info( $"Set TypeScript configuration '{ts.Name}' TargetProjectPath to '{ts.TargetProjectPath}'." );
                    }
                    else if( owner == owner.Owner?.FirstBinPath )
                    {
                        ts.TargetProjectPath = owner.ProjectPath.AppendPart( "Client" );
                        monitor.Info( $"Set first BinPath (named '{owner.Name}') empty TypeScript TargetProjectPath to '{ts.TargetProjectPath}'." );
                    }
                    else
                    {
                        monitor.Warn( $"Removing TypeScript configuration from BinPath '{owner.Name}' since its TargetProjectPath is empty:{Environment.NewLine}{ts.ToOnlyThisXml()}" );
                        owner.RemoveAspect( ts );
                        return false;
                    }
                }
                return true;
            }

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

        }

        static bool CheckPathOrTypeScriptSetDuplicate( IActivityMonitor monitor, IReadOnlyCollection<TypeScriptBinPathAspectConfiguration> configurations )
        {
            bool success = true;
            // This test is not perfect: the TargetProjectPath should be unique among all the TypeScript of all the BinPath.
            // Here we check only inside one but this is acceptable.
            var targetPath = configurations.GroupBy( c => c.TargetProjectPath.Path, StringComparer.OrdinalIgnoreCase );
            if( targetPath.Count() != configurations.Count )
            {
                foreach( var g in targetPath.Where( g => g.Count() > 1 ) )
                {
                    monitor.Error( $"TypeScript BinPath configuration with TargetProjectPath=\"{g.Key}\" appear more than once. " +
                                   $"Each configuration must target a different output path." );
                }
                success = false;
            }
            // The Name is an identifier (it is used in a folder, a TypeFilterName and a request path).
            // A client application requires a Name.
            foreach( var c in configurations )
            {
                if( c.Name != null && !IsValidName( c.Name ) )
                {
                    monitor.Error( $"Invalid TypeScript configuration Name '{c.Name}': it must contain only letters and digits and start with a letter." );
                    success = false;
                }
                if( c.Kind != ClientKind.None && c.Name == null )
                {
                    monitor.Error( $"TypeScript configuration with TargetProjectPath=\"{c.TargetProjectPath}\" is a client application (Kind=\"{c.Kind}\"): it requires a Name." );
                    success = false;
                }
                if( c.Kind != ClientKind.None && c.DevServerPort is not (> 0 and <= 65535) )
                {
                    monitor.Error( $"TypeScript configuration '{c.Name}' is a client application: it requires a DevServerPort (between 1 and 65535)." );
                    success = false;
                }
            }
            // In a BinPath (a host): the names and the request paths are unique, there is at most one default client application.
            foreach( var gBinPath in configurations.GroupBy( c => c.Owner ) )
            {
                foreach( var g in gBinPath.Where( c => c.Name != null ).GroupBy( c => c.Name!, StringComparer.OrdinalIgnoreCase ).Where( g => g.Count() > 1 ) )
                {
                    monitor.Error( $"TypeScript configuration Name \"{g.Key}\" appears more than once in BinPath '{gBinPath.Key!.Name}'." );
                    success = false;
                }
                foreach( var g in gBinPath.Where( c => c.RequestPath != null ).GroupBy( c => c.RequestPath!, StringComparer.OrdinalIgnoreCase ).Where( g => g.Count() > 1 ) )
                {
                    monitor.Error( $"TypeScript configurations '{g.Select( c => c.Name ).Concatenate( "', '" )}' of BinPath '{gBinPath.Key!.Name}' have the same request path \"{g.Key}\"." );
                    success = false;
                }
                foreach( var g in gBinPath.Where( c => c.Kind != ClientKind.None && c.DevServerPort != null ).GroupBy( c => c.DevServerPort ).Where( g => g.Count() > 1 ) )
                {
                    monitor.Error( $"Client applications '{g.Select( c => c.Name ).Concatenate( "', '" )}' of BinPath '{gBinPath.Key!.Name}' have the same DevServerPort {g.Key}." );
                    success = false;
                }
                var defaults = gBinPath.Where( c => c.Kind == ClientKind.DefaultApp ).ToList();
                if( defaults.Count > 1 )
                {
                    monitor.Error( $"BinPath '{gBinPath.Key!.Name}' has more than one default client application: '{defaults.Select( c => c.Name ).Concatenate( "', '" )}'." );
                    success = false;
                }
            }
            // This test is important: the TypeFilterName MUST be "None" or start with "TypeScript".
            var badNames = configurations.Where( c => c.TypeFilterName != "None" && !c.TypeFilterName.StartsWith( "TypeScript" ) );
            if( badNames.Any() )
            {
                monitor.Error( $"TypeScript configuration TypeFilterName MUST be or start with \"TypeScript\". " +
                               $"Following TypeFilterName are invalid: '{badNames.Select( c => c.TypeFilterName ).Concatenate( "', '" )}'." );
                success = false; ;
            }
            // This test is important: the TypeFilterName is registered (as an ExchangeableRuntimeFilter) and each set of types
            // must be uniquely identified (when they are not "None").
            // But this is only per BinPath.
            foreach( var gBinPath in configurations.Where( c => c.TypeFilterName != "None" ).GroupBy( c => c.Owner ) )
            {
                var filterNames = gBinPath.GroupBy( c => c.TypeFilterName, StringComparer.OrdinalIgnoreCase );
                if( filterNames.Count() != gBinPath.Count() )
                {
                    foreach( var g in filterNames.Where( g => g.Count() > 1 ) )
                    {
                        monitor.Error( $"TypeScript BinPath configuration with TypeFilterName=\"{g.Key}\" appear more than once in BinPath '{gBinPath.Key!.Path}'. " +
                                       $"They must use different names as they identify different set of types for the serialization layer." );
                    }
                    success = false;
                }
            }
            return success;
        }

        static bool IsValidName( string name ) => name.Length > 0 && char.IsAsciiLetter( name[0] ) && name.All( char.IsAsciiLetterOrDigit );

        static string NormalizeFolderPath( string p )
        {
            if( p.Length > 0 )
            {
                p = p.Replace( '\\', '/' );
                if( p[0] == '/' )
                {
                    if( p.Length == 1 ) return string.Empty;
                    p = p.Substring( 1, p.Length - 1 );
                }
                if( p[^1] == '/' ) return p;
                return p + '/';
            }
            return p;
        }

    }

    bool IStObjEngineAspect.OnSkippedRun( IActivityMonitor monitor ) => true;

    bool IStObjEngineAspect.RunPreCode( IActivityMonitor monitor, IStObjEngineRunContext context ) => true;

    CSCodeGenerationResult ICSCodeGenerator.Implement( IActivityMonitor monitor, ICSCodeGenerationContext c )
    {
        // Skips the purely unified BinPath.
        if( c.CurrentRun.ConfigurationGroup.IsUnifiedPure ) return CSCodeGenerationResult.Success;

        _runContexts.Clear();

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
        return new CSCodeGenerationResult( isJsonHere
                                            ? nameof( WaitForJsonSerialization )
                                            : nameof( WaitForLockedTypeSystem ) );
    }

    CSCodeGenerationResult WaitForLockedTypeSystem( IActivityMonitor monitor, ICSCodeGenerationContext c, [WaitFor] IPocoTypeSystem typeSystem )
    {
        using( monitor.OpenInfo( $"PocoTypeSystem is available (without Json serialization): handling TypeScript generation." ) )
        {
            return PrepareRun( monitor, c, typeSystem, null )
                    ? CSCodeGenerationResult.Success
                    : CSCodeGenerationResult.Failed;
        }
    }

    CSCodeGenerationResult WaitForJsonSerialization( IActivityMonitor monitor, ICSCodeGenerationContext c, [WaitFor] IPocoJsonSerializationServiceEngine jsonSerialization )
    {
        using( monitor.OpenInfo( $"IPocoJsonSerializationServiceEngine is available: handling TypeScript generation." ) )
        {
            return PrepareRun( monitor, c, jsonSerialization.SerializableLayer.TypeSystem, jsonSerialization )
                    ? CSCodeGenerationResult.Success
                    : CSCodeGenerationResult.Failed;
        }
    }

    bool PrepareRun( IActivityMonitor monitor, ICSCodeGenerationContext codeContext, IPocoTypeSystem typeSystem, IPocoJsonSerializationServiceEngine? jsonSerialization )
    {
        var binPath = codeContext.CurrentRun;
        using var _ = monitor.OpenInfo( $"Preparing TypeScript contexts for: {codeContext.CurrentRun.ConfigurationGroup.Names}." );

        // Obtains all the TypeScriptAspectConfiguration for all the BinPaths of the ConfigurationGroup.
        // One BinPath can have any number of TypeScriptAspectConfiguration. The TypeFilterName identifies
        // one configuration among the others in the same BinPath.
        var configs = binPath.ConfigurationGroup.SimilarConfigurations
                                    .SelectMany( c => c.FindAspect<TypeScriptBinPathAspectConfiguration>()?.AllConfigurations
                                                        ?? [] );

        // The type selection of a configuration (its Types, with the type name patterns, and its ExcludedTypes, the package
        // selection) is done by the TSContextInitializer.
        // What remains for the engine refactoring: the TypeScriptGroup/Package attributes should be EngineAttributes discovered
        // from the binPath.ConfigurationGroup.TypeSet (not ContextBoundDelegationAttributes read from the ITypeAttributesCache),
        // and the RegisterTypeScriptTypeAttribute (a RegisterPocoTypeAttribute of CK.StObj.Model) should follow. The type name
        // patterns are matched by scanning the TypeSet: a pattern lookup in the GlobalTypeCache would be the generic version.

        // Avoid creating one immutable lib per BinPath.
        // cachedKey is the last one.
        TypeScriptAspectConfiguration? cachedKey = null;
        ImmutableDictionary<string, SVersionBound>? cachedLibVersionsConfig = null;

        foreach( var tsBinPathConfig in configs )
        {
            Throw.DebugAssert( tsBinPathConfig.AspectConfiguration != null );

            var libVersionsConfig = cachedKey == tsBinPathConfig.AspectConfiguration
                                        ? cachedLibVersionsConfig
                                        : cachedLibVersionsConfig = (cachedKey = tsBinPathConfig.AspectConfiguration).LibraryVersions.ToImmutableDictionary();

            Throw.DebugAssert( libVersionsConfig != null );

            // First handles the configured <Types>, types that have [TypeScript] attribute or are decorated by some
            // ITSCodeGeneratorType and any type that are decorated with "global" ITSCodeGenerator. Then the discovered globals
            // ITSCodeGenerator.Initialize are called: new registered types can be added by global generators.
            // On success, the final TSContextInitializer.TypeScriptExchangeableSet is computed from all the registered types that
            // are IPocoType from the EmptyExchangeable set: this is an allow list.
            // => Only Poco compliant types that are reachable from a registered Poco type will be in TypeScriptExchangeableSet
            //    and handled by the PocoCodeGenerator.
            var initializer = TSContextInitializer.Create( monitor,
                                                           binPath,
                                                           tsBinPathConfig,
                                                           libVersionsConfig,
                                                           typeSystem.SetManager.AllExchangeable,
                                                           jsonSerialization );
            if( initializer == null ) return false;

            // We now have the Global code generators initialized, the configured attributes on explicitly registered types,
            // discovered types or newly added types and a set of "TypeScriptExchangeable" Poco types.
            IPocoTypeNameMap? exchangeableNames = null;
            if( jsonSerialization != null && tsBinPathConfig.TypeFilterName != "None" )
            {
                // If Json serialization is available, let's get the name map for them.
                // It the sets differ, build a dedicated name map for it (Note: this cannot be a subset of the names
                // because of anonymous record names that expose their fields).
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
                jsonSerialization.SerializableLayer.RegisterExchangeableRuntimeFilter( monitor, tsBinPathConfig.TypeFilterName, initializer.TypeScriptExchangeableSet );
            }
            // The TypeScriptContext for this configuration can now be initialized.
            // It will be run by FinalImplement.
            _runContexts.Add( new TypeScriptContext( codeContext, tsBinPathConfig, initializer, exchangeableNames ) );
        }
        if( _runContexts.Count == 0 )
        {
            monitor.Info( $"Skipped TypeScript generation for BinPathConfiguration '{binPath.ConfigurationGroup.Names}': no TypeScript BinPath configuration." );
        }
        return true;
    }

    bool ICSCodeGeneratorWithFinalization.FinalImplement( IActivityMonitor monitor, ICSCodeGenerationContext codeContext )
    {
        using var _ = monitor.OpenInfo( $"Running TypeScript contexts for: {codeContext.CurrentRun.ConfigurationGroup.Names}." );
        foreach( var g in _runContexts )
        {
            if( !g.Run( monitor ) )
            {
                return false;
            }
            // Save or defer.
            if( _deferedSave != null )
            {
                monitor.Info( "Deferring files save and target project integration." );
                _deferedSave.Add( g );
            }
            else if( !g.Save( monitor ) ) return false;
        }
        return true;
    }


    bool IStObjEngineAspect.RunPostCode( IActivityMonitor monitor, IStObjEnginePostCodeRunContext context ) => true;

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
