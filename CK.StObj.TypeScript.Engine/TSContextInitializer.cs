using CK.Core;
using CK.Setup.PocoJson;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.Setup
{
    sealed class TSContextInitializer
    {
        readonly Dictionary<Type, RegisteredType> _registeredTypes;
        readonly List<ITSCodeGenerator> _globals;
        readonly IPocoTypeSet _typeScriptExchangeableSet;

        public IReadOnlyDictionary<Type, RegisteredType> RegisteredTypes => _registeredTypes;

        public IReadOnlyList<ITSCodeGenerator> GlobalCodeGenerators => _globals;

        public IPocoTypeSet TypeScriptExchangeableSet => _typeScriptExchangeableSet;

        public static TSContextInitializer? Create( IActivityMonitor monitor,
                                                    ICodeGenerationContext codeGenContext,
                                                    TypeScriptAspectConfiguration configuration,
                                                    TypeScriptAspectBinPathConfiguration binPathConfiguration,
                                                    IPocoTypeSet allExchangeableSet,
                                                    IPocoJsonSerializationServiceEngine? jsonSerialization )
        {
            if( BuildRegTypesFromConfiguration( monitor, binPathConfiguration, allExchangeableSet, out var regTypes )
                && BuildRegTypesFromAttributesAndDiscoverGenerators( monitor,
                                                                     regTypes,
                                                                     codeGenContext.CurrentRun.EngineMap.AllTypesAttributesCache.Values,
                                                                     allExchangeableSet,
                                                                     out var globals )
                && InitializeGlobalGenerators( monitor,
                                               codeGenContext,
                                               configuration,
                                               binPathConfiguration,
                                               globals,
                                               regTypes,
                                               allExchangeableSet,
                                               jsonSerialization ) )
            {
                IPocoTypeSystem typeSystem = allExchangeableSet.TypeSystem;
                var emptyExchangeableSet = typeSystem.SetManager.EmptyExchangeable;
                // Creating the TypeScript exchangeable set based on EmptyExchangeable with all the registered types.
                // We exclude the UserMessage and the FormattedString structs and the MCString and CodeString reference types
                // because there are no TypeScript implementation for them (at least for now), only the SimpleUserMessage
                // (that will be handled as any other named record) is TypeScript compliant.
                var include = regTypes.Values.Select( r => r.PocoType ).Where( p => p != null );
                var exclude = (new[] {
                    typeSystem.FindByType( typeof( UserMessage ) ),
                    typeSystem.FindByType( typeof( FormattedString ) ),
                    typeSystem.FindByType( typeof( MCString ) ),
                    typeSystem.FindByType( typeof( CodeString ) ),
                }).Where( t => t != null );

                var tsExchangeable = emptyExchangeableSet.IncludeAndExclude( include!, exclude! );
                monitor.Info( $"Registered {tsExchangeable.NonNullableTypes.Count} exchangeable Poco types out of {regTypes.Count} types to register." );
                return new TSContextInitializer( regTypes, globals, tsExchangeable );
            }
            return null;
        }

        TSContextInitializer( Dictionary<Type, RegisteredType> r, List<ITSCodeGenerator> g, IPocoTypeSet s )
        {
            _registeredTypes = r;
            _globals = g;
            _typeScriptExchangeableSet = s;
        }

        // Step 1.
        static bool BuildRegTypesFromConfiguration( IActivityMonitor monitor,
                                                    TypeScriptAspectBinPathConfiguration binPathConfiguration,
                                                    IPocoTypeSet allExchangeableSet,
                                                    out Dictionary<Type, RegisteredType> registeredTypes )
        {
            registeredTypes = new Dictionary<Type, RegisteredType>();
            using( monitor.OpenInfo( $"Building TypeScriptAttribute for {binPathConfiguration.Types.Count} Type configurations." ) )
            {
                bool success = true;
                foreach( TypeScriptTypeConfiguration c in binPathConfiguration.Types )
                {
                    Type? t = FindType( allExchangeableSet.TypeSystem.PocoDirectory, c.Type );
                    if( t == null )
                    {
                        monitor.Error( $"Unable to resolve type '{c.Type}' in TypeScriptAspectConfiguration:{Environment.NewLine}{c.ToXml()}" );
                        success = false;
                    }
                    else
                    {
                        var attr = c.ToAttribute( monitor, typeName => FindType( allExchangeableSet.TypeSystem.PocoDirectory, typeName ) );
                        if( attr == null ) success = false;
                        else
                        {
                            // If the configured type is a PocoType, then it MUST belong to the exchangeable set of types.
                            // If the configured type is a not a PocoType, we consider it as a simple C# type.
                            var pT = allExchangeableSet.TypeSystem.FindByType( t );
                            if( pT == null )
                            {
                                monitor.Warn( $"Type '{t:N}' is not registered in PocoTypeSystem." );
                            }
                            else
                            {
                                pT = pT.NonNullable;
                                if( !allExchangeableSet.Contains( pT ) )
                                {
                                    // If it is a IPocoType, then it must be exchangeable.
                                    // This is an error since it appears in the configuration.
                                    monitor.Error( $"Poco type '{pT}' is not exchangeable in TypeScriptAspectConfiguration:{Environment.NewLine}{c.ToXml()}" );
                                    success = false;
                                }
                            }
                            if( success )
                            {
                                registeredTypes.Add( t, new RegisteredType( null, pT, attr ) );
                            }
                        }
                    }
                }
                return success;
            }

            static Type? FindType( IPocoDirectory pocoDirectory, string typeName )
            {
                var t = SimpleTypeFinder.WeakResolver( typeName, false );
                if( t == null && pocoDirectory.NamedFamilies.TryGetValue( typeName, out var rootInfo ) )
                {
                    t = rootInfo.PrimaryInterface.PocoInterface;
                }
                return t;
            }
        }

        // Step 2.
        static bool BuildRegTypesFromAttributesAndDiscoverGenerators( IActivityMonitor monitor,
                                                                      Dictionary<Type, RegisteredType> registeredTypes,
                                                                      IEnumerable<ITypeAttributesCache> attributes,
                                                                      IPocoTypeSet allExchangeableSet,
                                                                      out List<ITSCodeGenerator> globals )
        {
            globals = new List<ITSCodeGenerator>();
            using( monitor.OpenInfo( "Analyzing types with [TypeScript] and/or ITSCodeGeneratorType or ITSCodeGenerator attributes." ) )
            {
                // These variables are reused per type.
                TypeScriptAttributeImpl? tsAttrImpl;
                List<ITSCodeGeneratorType> generators = new List<ITSCodeGeneratorType>();

                bool success = true;
                foreach( ITypeAttributesCache attributeCache in attributes )
                {
                    tsAttrImpl = null;
                    generators.Clear();

                    foreach( var m in attributeCache.GetTypeCustomAttributes<ITSCodeGeneratorAutoDiscovery>() )
                    {
                        if( m is ITSCodeGenerator g )
                        {
                            globals.Add( g );
                        }
                        if( m is TypeScriptAttributeImpl a )
                        {
                            if( tsAttrImpl != null )
                            {
                                monitor.Error( $"Multiple TypeScriptAttribute decorates '{attributeCache.Type}'." );
                                success = false;
                            }
                            tsAttrImpl = a;
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
                        TypeScriptAttribute? configuredAttr = reg.Attribute;
                        TypeScriptAttribute? a = tsAttrImpl?.Attribute.ApplyOverride( configuredAttr ) ?? configuredAttr;
                        IPocoType? pocoType = reg.PocoType;
                        // If the type is configured then its configuredAttr is not null: the work on whether it is
                        // a IPocoType has been done by step 1.
                        // But if the type was not in the configuration (it has only a [TypeScript] or has a TSGeneratorType attribute),
                        // then we check whether it is a IPocoType or not.
                        // If it is, we associate its IPocoType only it it belongs to the exhangeable set.
                        // If it doesn't belong to the exchangeable set, we have to decide if:
                        // - We accept it: by setting the RegType.PocoType, we will add it to the final TypeScript Poco set... This is not an option: the 
                        //   TypeScript Poco set will no more be a subset of the Poco exchangeable set. This breaks an invariant and we cannot anymore reason
                        //   about the System.
                        // - We raise an error: a PocoType that has been marked as NonExchangeable and has a [TypeScript] or has a TSGeneratorType attribute
                        //   is invalid and breaks the system.
                        // - Or we don't assign the RegType.PocoType and let the type be considered a simple C# type. If a TSCodeGenerator can handle it, then
                        //   everything is fine but the PocoCodeGenerator will simply ignore it. If no code generator handle it, this will be an error.
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

        sealed class Initializer : ITypeScriptContextInitializer
        {
            readonly ICodeGenerationContext _codeGenContext;
            readonly TypeScriptAspectConfiguration _configuration;
            readonly TypeScriptAspectBinPathConfiguration _binPathConfiguration;
            readonly IReadOnlyList<ITSCodeGenerator> _globals;
            readonly Dictionary<Type, RegisteredType> _regTypes;
            readonly IPocoJsonSerializationServiceEngine? _jsonSerialization;
            readonly IPocoTypeSet _allExchangeableSet;

            public Initializer( ICodeGenerationContext codeGenContext,
                                TypeScriptAspectConfiguration configuration,
                                TypeScriptAspectBinPathConfiguration binPathConfiguration,
                                IReadOnlyList<ITSCodeGenerator> globals,
                                Dictionary<Type, RegisteredType> regTypes,
                                IPocoJsonSerializationServiceEngine? jsonSerialization,
                                IPocoTypeSet allExchangeableSet )
            {
                _codeGenContext = codeGenContext;
                _configuration = configuration;
                _binPathConfiguration = binPathConfiguration;
                _globals = globals;
                _regTypes = regTypes;
                _jsonSerialization = jsonSerialization;
                _allExchangeableSet = allExchangeableSet;
            }

            public ICodeGenerationContext CodeGenContext => _codeGenContext;

            public IReadOnlyList<ITSCodeGenerator> GlobalGenerators => _globals;

            public IReadOnlyDictionary<Type, RegisteredType> RegisteredTypes => _regTypes;

            public IPocoTypeSystem PocoTypeSystem => _allExchangeableSet.TypeSystem;

            public IPocoTypeSet AllExchangeableSet => _allExchangeableSet;

            public IPocoJsonSerializationServiceEngine? JsonSerialization => _jsonSerialization;

            public TypeScriptAspectConfiguration Configuration => _configuration;

            public TypeScriptAspectBinPathConfiguration BinPathConfiguration => _binPathConfiguration;

            public bool EnsureRegister( IActivityMonitor monitor,
                                        Type t,
                                        bool mustBePocoType,
                                        Func<TypeScriptAttribute?, TypeScriptAttribute?>? attributeConfigurator = null )
            {
                if( _regTypes.TryGetValue( t, out RegisteredType regType ) )
                {
                    var a = attributeConfigurator?.Invoke( regType.Attribute );
                    if( a != regType.Attribute )
                    {
                        _regTypes[t] = new RegisteredType( regType.Generators, regType.PocoType, a );
                    }
                    return true;
                }
                var pocoType = CheckPocoType( monitor, _allExchangeableSet, t, out bool isPocoType );
                if( pocoType == null && isPocoType && mustBePocoType )
                {
                    monitor.Error( $"Poco type '{pocoType}' is not exchangeable. It cannot be registered." );
                    return false;
                }
                var attr = attributeConfigurator?.Invoke( null );
                _regTypes.Add( t, new RegisteredType( Array.Empty<ITSCodeGeneratorType>(), pocoType, attr ) );
                return true;
            }
        }

        // Step 3.
        static bool InitializeGlobalGenerators( IActivityMonitor monitor,
                                                ICodeGenerationContext codeGenContext,
                                                TypeScriptAspectConfiguration configuration,
                                                TypeScriptAspectBinPathConfiguration binPathConfiguration,
                                                List<ITSCodeGenerator> globals,
                                                Dictionary<Type, RegisteredType> regTypes,
                                                IPocoTypeSet allExchangeableSet,
                                                IPocoJsonSerializationServiceEngine? jsonSerialization )
        {
            var i = new Initializer( codeGenContext, configuration, binPathConfiguration, globals, regTypes, jsonSerialization, allExchangeableSet );
            return CallGlobalCodeGenerators( monitor, globals, i, null );
        }

        internal static bool CallGlobalCodeGenerators( IActivityMonitor monitor,
                                                       IReadOnlyList<ITSCodeGenerator> globals,
                                                       ITypeScriptContextInitializer? initializer,
                                                       TypeScriptContext? context )
        {
            Throw.DebugAssert( (initializer == null) != (context == null) );
            string action = initializer != null ? "Initializing" : "StartCodeGeneration for";
            using( monitor.OpenInfo( $"{action} the {globals.Count} global {nameof( ITSCodeGenerator )} TypeScript generators." ) )
            {
                var success = true;
                foreach( var global in globals )
                {
                    using( monitor.OpenTrace( $"{action} '{global.GetType():N}' global TypeScript generator." ) )
                    {
                        try
                        {
                            success &= initializer != null
                                        ? global.Initialize( monitor, initializer )
                                        : global.StartCodeGeneration( monitor, context! );
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( ex );
                            success = false;
                        }
                    }
                }
                if( !success )
                {
                    monitor.CloseGroup( "Failed." );
                    return false;
                }
            }
            return true;
        }

    }
}
