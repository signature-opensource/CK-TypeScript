using CK.Core;
using CK.Setup.PocoJson;
using CK.TypeScript;
using CK.TypeScript.Engine;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Setup;

sealed class TSContextInitializer
{
    readonly Dictionary<Type, RegisteredType> _registeredTypes;
    readonly ImmutableArray<ITSCodeGenerator> _globals;
    readonly IPocoTypeSet _typeScriptExchangeableSet;
    readonly TypeScriptIntegrationContext? _integrationContext;
    readonly ImmutableDictionary<string, SVersionBound> _libVersionsConfig;
    readonly IReadOnlyList<TypeScriptGroupOrPackageAttributeImpl> _packages;
    readonly IDictionary<object, object?>? _rootMemory;

    /// <summary>
    /// Gets the types that have been explictely registered.
    /// </summary>
    public IReadOnlyDictionary<Type, RegisteredType> RegisteredTypes => _registeredTypes;

    /// <summary>
    /// Gets the global TypeScript generators.
    /// </summary>
    public ImmutableArray<ITSCodeGenerator> GlobalCodeGenerators => _globals;

    /// <summary>
    /// Gets the TypeScriptGroupOrPackageAttributeImpl.
    /// </summary>
    public IReadOnlyList<TypeScriptGroupOrPackageAttributeImpl> Packages => _packages;

    /// <summary>
    /// Gets the set of Poco compliant types that must be handled in TypeScript.
    /// </summary>
    public IPocoTypeSet TypeScriptExchangeableSet => _typeScriptExchangeableSet;

    /// <summary>
    /// Gets the intergation context if integration mode is not null.
    /// </summary>
    public TypeScriptIntegrationContext? IntegrationContext => _integrationContext;

    /// <summary>
    /// Gets the root memory if it has been initialized.
    /// </summary>
    public IDictionary<object, object?>? RootMemory => _rootMemory;

    /// <summary>
    /// Gets the library configured versions.
    /// </summary>
    public ImmutableDictionary<string, SVersionBound> LibVersionsConfig => _libVersionsConfig;

    public static TSContextInitializer? Create( IActivityMonitor monitor,
                                                IGeneratedBinPath genBinPath,
                                                TypeScriptBinPathAspectConfiguration binPathConfiguration,
                                                ImmutableDictionary<string, SVersionBound> libVersionsConfig,
                                                IPocoTypeSet allExchangeableSet,
                                                IPocoJsonSerializationServiceEngine? jsonSerialization )
    {
        if( BuildRegTypesFromConfiguration( monitor, binPathConfiguration, allExchangeableSet, out var regTypes )
            && BuildRegTypesFromAttributesAndDiscoverGenerators( monitor,
                                                                 regTypes,
                                                                 genBinPath.EngineMap.AllTypesAttributesCache.Values,
                                                                 allExchangeableSet,
                                                                 out var globalFactories,
                                                                 out var packages )
            && InitializeIntegrationContext( monitor, binPathConfiguration, libVersionsConfig, out var integrationContext )
            && InitializeGlobalGeneratorsAndPackages( monitor,
                                                      binPathConfiguration,
                                                      integrationContext,
                                                      regTypes,
                                                      allExchangeableSet,
                                                      jsonSerialization,
                                                      packages,
                                                      globalFactories,
                                                      out var globals,
                                                      out var rootMemory ) )
        {
            IPocoTypeSystem typeSystem = allExchangeableSet.TypeSystem;
            var emptyExchangeableSet = typeSystem.SetManager.EmptyExchangeable;
            IPocoTypeSet tsExchangeable;
            // Creating the TypeScript exchangeable set based on EmptyExchangeable with all the registered types.
            // We exclude the FormattedString structs and the MCString and CodeString reference types
            // because there are no TypeScript implementation for them (at least for now), only the SimpleUserMessage
            // (that will be handled as any other named record) is TypeScript compliant.
            //
            // The UserMessage is accepted: we map it to the SimpleUserMessage type.
            //
            if( binPathConfiguration.TypeFilterName != "None" )
            {
                var include = regTypes.Values.Select( r => r.PocoType ).Where( p => p != null );
                var exclude = (new[] { typeSystem.FindByType( typeof( FormattedString ) ),
                                       typeSystem.FindByType( typeof( MCString ) ),
                                       typeSystem.FindByType( typeof( CodeString ) ),
                                    }).Where( t => t != null );
                tsExchangeable = emptyExchangeableSet.IncludeAndExclude( include!, exclude! );
                monitor.Info( $"Registered {tsExchangeable.NonNullableTypes.Count} exchangeable Poco types (out of {regTypes.Count} types to register)." );
            }
            else
            {
                monitor.Info( $"No exchangeable Poco types will be considered because TypeFilterName is \"None\"." );
                tsExchangeable = emptyExchangeableSet;
            }
            return new TSContextInitializer( regTypes,
                                             globals,
                                             tsExchangeable,
                                             integrationContext,
                                             libVersionsConfig,
                                             packages,
                                             rootMemory );
        }
        return null;
    }

    TSContextInitializer( Dictionary<Type, RegisteredType> r,
                          ImmutableArray<ITSCodeGenerator> g,
                          IPocoTypeSet s,
                          TypeScriptIntegrationContext? integrationContext,
                          ImmutableDictionary<string, SVersionBound> libVersionsConfig,
                          IReadOnlyList<TypeScriptGroupOrPackageAttributeImpl> packages,
                          IDictionary<object, object?>? rootMemory )
    {
        _registeredTypes = r;
        _globals = g;
        _typeScriptExchangeableSet = s;
        _integrationContext = integrationContext;
        _libVersionsConfig = libVersionsConfig;
        _packages = packages;
        _rootMemory = rootMemory;
    }

    // Step 1.
    static bool BuildRegTypesFromConfiguration( IActivityMonitor monitor,
                                                TypeScriptBinPathAspectConfiguration binPathConfiguration,
                                                IPocoTypeSet allExchangeableSet,
                                                out Dictionary<Type, RegisteredType> registeredTypes )
    {
        registeredTypes = new Dictionary<Type, RegisteredType>();
        using( monitor.OpenInfo( $"Building TypeScriptAttribute for {binPathConfiguration.Types.Count + binPathConfiguration.GlobTypes.Count + binPathConfiguration.ExcludedTypes.Count} Type configurations." ) )
        {
            bool success = true;
            foreach( var (type, attr) in binPathConfiguration.Types )
            {
                // If the configured type is a PocoType, then it MUST belong to the exchangeable set of types.
                // If the configured type is a not a PocoType, we consider it as a simple C# type (that must
                // eventually be handled by a TypeScript code generator).
                var pocoType = allExchangeableSet.TypeSystem.FindByType( type );
                if( pocoType != null )
                {
                    pocoType = pocoType.NonNullable;
                    if( !allExchangeableSet.Contains( pocoType ) )
                    {
                        // If it is a IPocoType, then it must be exchangeable.
                        // This is an error since it appears in the configuration.
                        monitor.Error( $"Registered Poco type '{pocoType}' is not exchangeable." );
                        success = false;
                    }
                }
                if( success )
                {
                    registeredTypes.Add( type, new RegisteredType( null, pocoType, attr ) );
                }
            }
            return success;
        }
    }

    // Step 2.
    static bool BuildRegTypesFromAttributesAndDiscoverGenerators( IActivityMonitor monitor,
                                                                  Dictionary<Type, RegisteredType> registeredTypes,
                                                                  IEnumerable<ITypeAttributesCache> attributes,
                                                                  IPocoTypeSet allExchangeableSet,
                                                                  out List<ITSCodeGeneratorFactory> globals,
                                                                  out List<TypeScriptGroupOrPackageAttributeImpl> packages )
    {
        globals = new List<ITSCodeGeneratorFactory>();
        packages = new List<TypeScriptGroupOrPackageAttributeImpl>();
        using( monitor.OpenInfo( "Analyzing types with [TypeScript], [TypeScriptGroup/Package] and/or ITSCodeGeneratorType or ITSCodeGeneratorFactory attributes." ) )
        {
            // These variables are reused per type.
            TypeScriptTypeAttributeImpl? tsAttrImpl;
            var generators = new List<ITSCodeGeneratorType>();

            bool success = true;
            foreach( ITypeAttributesCache attributeCache in attributes )
            {
                tsAttrImpl = null;
                generators.Clear();
                int typeScriptPackageAttrCount = 0;

                foreach( var m in attributeCache.GetTypeCustomAttributes<ITSCodeGeneratorAutoDiscovery>() )
                {
                    if( m is TypeScriptTypeAttributeImpl a )
                    {
                        if( tsAttrImpl != null )
                        {
                            monitor.Error( $"Multiple TypeScriptAttribute decorate '{attributeCache.Type:N}'." );
                            success = false;
                        }
                        tsAttrImpl = a;
                    }
                    else if( m is TypeScriptGroupOrPackageAttributeImpl p )
                    {
                        if( ++typeScriptPackageAttrCount == 2 )
                        {
                            monitor.Error( $"""
                                    TypeScript package '{attributeCache.Type:N}' is decorated with more than one [TypeScriptPackage] or specialized attribute:
                                    [{attributeCache.GetTypeCustomAttributes<TypeScriptGroupOrPackageAttributeImpl>().Select( a => a.Attribute.GetType().Name ).Concatenate( "], [" )}]
                                    """ );
                            success = false;
                        }
                        packages.Add( p );
                    }
                    if( m is ITSCodeGeneratorFactory g )
                    {
                        globals.Add( g );
                    }
                    if( m is ITSCodeGeneratorType tG )
                    {
                        generators.Add( tG );
                    }
                }
                // If the attribute is only a ITSCodeGeneratorType, we let the RegType.Attribute be null.
                if( tsAttrImpl != null || generators.Count > 0 )
                {
                    // Did this type appear in the configuration?
                    // If yes, the configuration must override the values from the code.
                    RegisteredType reg = registeredTypes.GetValueOrDefault( attributeCache.Type );
                    TypeScriptTypeAttribute? configuredAttr = reg.Attribute;
                    TypeScriptTypeAttribute? a = tsAttrImpl?.Attribute.ApplyOverride( configuredAttr ) ?? configuredAttr;
                    IPocoType? pocoType = reg.PocoType;
                    // If the type is configured then its configuredAttr is not null: the work on whether it is
                    // a IPocoType has been done by step 1.
                    // But if the type was not in the configuration (it has only a [TypeScript] or has a ITSGeneratorType attribute),
                    // then we check whether it is a IPocoType or not.
                    // If it is, we associate its IPocoType only it it belongs to the exchangeable set.
                    // If it doesn't belong to the exchangeable set, we have to decide if:
                    // - We accept it: by setting the RegType.PocoType, we will add it to the final TypeScript Poco set...
                    // This is not possible: the TypeScript Poco set will no more be a subset of the Poco exchangeable set.
                    // This breaks an invariant and we cannot anymore reason about the System.
                    // - We raise an error: a PocoType that has been marked as NonExchangeable and has a [TypeScript] or has
                    //   a ITSGeneratorType attribute is invalid and breaks the system.
                    // - Or we don't assign the RegType.PocoType and let the type be a simple C# type. If a TSCodeGenerator can handle it, then
                    //   everything is fine but the PocoCodeGenerator will simply ignore it.
                    //   If no code generator handle it, this will be an error.
                    //
                    // The last option is definitely the best. This leaves room for edge cases and keeps the TypeScript Poco set logically sound.
                    //
                    if( configuredAttr == null && pocoType == null )
                    {
                        pocoType = CheckPocoType( monitor, allExchangeableSet, attributeCache.Type, out _ );
                    }
                    registeredTypes[attributeCache.Type] = new RegisteredType( generators.Count > 0 ? generators.ToArray() : null, pocoType, a );
                }
            }
            if( success ) monitor.CloseGroup( $"Found {globals.Count} global generators and {registeredTypes.Count} types to consider." );
            return success;
        }
    }

    static IPocoType? CheckPocoType( IActivityMonitor monitor, IPocoTypeSet allExchangeableSet, Type type, out bool isPocoType )
    {
        IPocoType? pocoType = allExchangeableSet.TypeSystem.FindByType( type );
        if( pocoType == null )
        {
            isPocoType = false;
            monitor.Trace( $"Type '{type:N}' is a regular C# type (not registered in the PocoTypeSystem)." );
        }
        else
        {
            isPocoType = true;
            if( !allExchangeableSet.Contains( pocoType ) )
            {
                // The Type is [TypeScript] or has a TSGeneratorType and is a IPocoType but is not exchangeable.
                monitor.Warn( $"Poco type '{pocoType}' is not exchangeable. Considering it as a regular C# type." );
                pocoType = null;
            }
        }
        return pocoType;
    }

    // Step 3
    static bool InitializeIntegrationContext( IActivityMonitor monitor,
                                              TypeScriptBinPathAspectConfiguration binPathConfiguration,
                                              ImmutableDictionary<string, SVersionBound> libVersionsConfig,
                                              out TypeScriptIntegrationContext? integrationContext )
    {
        integrationContext = null;
        if( binPathConfiguration.IntegrationMode != CKGenIntegrationMode.None )
        {
            integrationContext = TypeScriptIntegrationContext.Create( monitor, binPathConfiguration, libVersionsConfig );
            if( integrationContext == null ) return false;
        }
        return true;
    }


    sealed class Initializer : ITypeScriptContextInitializer
    {
        readonly TypeScriptBinPathAspectConfiguration _binPathConfiguration;
        readonly TypeScriptIntegrationContext? _integrationContext;
        readonly Dictionary<Type, RegisteredType> _regTypes;
        readonly IPocoJsonSerializationServiceEngine? _jsonSerialization;
        readonly IPocoTypeSet _allExchangeableSet;
        readonly IReadOnlyList<TypeScriptGroupOrPackageAttributeImpl> _packages;
        Dictionary<object,object?>? _rootMemory;

        public Initializer( TypeScriptBinPathAspectConfiguration binPathConfiguration,
                            TypeScriptIntegrationContext? integrationContext,
                            Dictionary<Type, RegisteredType> regTypes,
                            IPocoJsonSerializationServiceEngine? jsonSerialization,
                            IPocoTypeSet allExchangeableSet,
                            IReadOnlyList<TypeScriptGroupOrPackageAttributeImpl> packages )
        {
            _binPathConfiguration = binPathConfiguration;
            _integrationContext = integrationContext;
            _regTypes = regTypes;
            _jsonSerialization = jsonSerialization;
            _allExchangeableSet = allExchangeableSet;
            _packages = packages;
        }

        public IReadOnlyDictionary<Type, RegisteredType> RegisteredTypes => _regTypes;

        public IPocoTypeSystem PocoTypeSystem => _allExchangeableSet.TypeSystem;

        public IPocoTypeSet AllExchangeableSet => _allExchangeableSet;

        public IPocoJsonSerializationServiceEngine? JsonSerialization => _jsonSerialization;

        public TypeScriptBinPathAspectConfiguration BinPathConfiguration => _binPathConfiguration;

        public TypeScriptIntegrationContext? IntegrationContext => _integrationContext;

        public IReadOnlyList<TypeScriptGroupOrPackageAttributeImpl> Packages => _packages;

        // Lazy instantiation of the RootMemory.
        internal Dictionary<object, object?>? RootMemory => _rootMemory;

        IDictionary<object, object?> ITypeScriptContextInitializer.RootMemory => _rootMemory ??= new Dictionary<object,object?>();

        public bool EnsureRegister( IActivityMonitor monitor,
                                    Type t,
                                    bool mustBePocoType,
                                    Func<TypeScriptTypeAttribute?, TypeScriptTypeAttribute?>? attributeConfigurator = null )
        {
            // If the type is already registered, applies the attributeConfigurator and updates the registration.
            if( _regTypes.TryGetValue( t, out RegisteredType regType ) )
            {
                if( attributeConfigurator != null )
                {
                    var a = attributeConfigurator?.Invoke( regType.Attribute );
                    if( a != regType.Attribute )
                    {
                        _regTypes[t] = new RegisteredType( regType.Generators, regType.PocoType, a );
                    }
                }
                return true;
            }
            // Not yet registered: checks its IPoco status.
            var pocoType = CheckPocoType( monitor, _allExchangeableSet, t, out bool isPocoType );
            if( mustBePocoType && pocoType == null )
            {
                if( isPocoType )
                {
                    monitor.Error( $"Type '{t:C}' is a Poco but is not exchangeable. It cannot be registered in TypeScript." );
                }
                else
                {
                    monitor.Error( $"Type '{t:C}' is not a registered Poco. It cannot be registered in TypeScript." );
                }
                return false;
            }
            var attr = attributeConfigurator?.Invoke( null );
            _regTypes.Add( t, new RegisteredType( Array.Empty<ITSCodeGeneratorType>(), pocoType, attr ) );
            return true;
        }
    }

    // Step 4.
    static bool InitializeGlobalGeneratorsAndPackages( IActivityMonitor monitor,
                                                       TypeScriptBinPathAspectConfiguration binPathConfiguration,
                                                       TypeScriptIntegrationContext? integrationContext,
                                                       Dictionary<Type, RegisteredType> regTypes,
                                                       IPocoTypeSet allExchangeableSet,
                                                       IPocoJsonSerializationServiceEngine? jsonSerialization,
                                                       List<TypeScriptGroupOrPackageAttributeImpl> packages,
                                                       List<ITSCodeGeneratorFactory> globalFactories,
                                                       out ImmutableArray<ITSCodeGenerator> globals,
                                                       out IDictionary<object,object?>? rootMemory )
    {
        var i = new Initializer( binPathConfiguration, integrationContext, regTypes, jsonSerialization, allExchangeableSet, packages );
        using( monitor.OpenInfo( $"Creating the {globalFactories.Count} global {nameof( ITSCodeGenerator )} TypeScript generators, initializing {packages.Count} TypeScript packages." ) )
        {
            bool success = true;
            var b = ImmutableArray.CreateBuilder<ITSCodeGenerator>( globalFactories.Count );
            foreach( var f in globalFactories )
            {
                var g = f.CreateTypeScriptGenerator( monitor, i );
                if( g == null )
                {
                    success = false;
                }
                else
                {
                    b.Add( g );
                }
            }
            if( success )
            {
                foreach( var p in packages )
                {
                    success &= p.HandleRegisterTypeScriptTypeAttributes( monitor, i );
                }
                if( success )
                {
                    globals = b.MoveToImmutable();
                    rootMemory = i.RootMemory;
                    return true;
                }
            }
            globals = ImmutableArray<ITSCodeGenerator>.Empty;
            rootMemory = null;
            return false;
        }
    }

}
