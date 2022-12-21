using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.Setup.PocoJson;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Central class that handles TypeScript generation in a <see cref="TypeScriptRoot"/> (the <see cref="Root"/>)
    /// and <see cref="ICodeGenerationContext"/> (the <see cref="CodeContext"/>).
    /// <para>
    /// This is instantiated and made available to the participants (<see cref="ITSCodeGenerator"/> and <see cref="ITSCodeGeneratorType"/>)
    /// only if the configuration actually allows the TypeScript generation for this <see cref="CodeContext"/>.
    /// </para>
    /// </summary>
    public sealed class TypeScriptContext
    {
        readonly Dictionary<Type, TSDecoratedType> _typeDecorators;
        readonly IReadOnlyDictionary<Type, ITypeAttributesCache> _attributeCache;
        readonly IPocoTypeSystem _pocoTypeSystem;
        readonly List<ITSCodeGenerator> _globals;
        bool _success;

        internal TypeScriptContext( IReadOnlyCollection<(NormalizedPath Path, XElement Config)> outputPaths,
                                    ICodeGenerationContext codeCtx,
                                    TypeScriptAspectConfiguration config,
                                    IPocoTypeSystem pocoTypeSystem,
                                    ExchangeableTypeNameMap? jsonNames )
        {
            Root = new TypeScriptContextRoot( this, outputPaths, config );
            CodeContext = codeCtx;
            _pocoTypeSystem = pocoTypeSystem;
            JsonNames = jsonNames;
            _typeDecorators = new Dictionary<Type, TSDecoratedType>();
            _attributeCache = codeCtx.CurrentRun.EngineMap.AllTypesAttributesCache;
            _globals = new List<ITSCodeGenerator>();
            _success = true;
        }

        /// <summary>
        /// Gets the TypeScript code generation root.
        /// </summary>
        public TypeScriptContextRoot Root { get; }

        /// <summary>
        /// Gets the <see cref="ICodeGenerationContext"/> that is being processed.
        /// </summary>
        public ICodeGenerationContext CodeContext { get; }

        /// <summary>
        /// Gets the Json <see cref="ExchangeableTypeNameMap"/> if it is available.
        /// </summary>
        public ExchangeableTypeNameMap? JsonNames { get; }

        /// <summary>
        /// Gets all the global generators.
        /// </summary>
        public IReadOnlyList<ITSCodeGenerator> GlobalGenerators => _globals;

        internal bool Run( IActivityMonitor monitor )
        {
            using( monitor.OpenInfo( "Running TypeScript code generation." ) )
            {
                return DiscoverGeneratorsAndTypeScriptAttributes( monitor )
                       && InitializeGlobalGenerators( monitor )
                       && RegisterAllExchangeablePocoType( monitor )
                       && CallCodeGenerators( monitor, initialize: true )
                       && CallCodeGenerators( monitor, false );
            }
        }

        /// <summary>
        /// Step 1: Discovering the generators and TypeScript attributes thanks to ITSCodeGeneratorAutoDiscovery.
        ///         Registers the globals and Type bound generators.
        /// </summary>
        bool DiscoverGeneratorsAndTypeScriptAttributes( IActivityMonitor monitor )
        {
            _globals.Add( new PocoCodeGenerator( _pocoTypeSystem ) );

            // These variables are reused per type.
            TypeScriptAttributeImpl? attr;
            List<ITSCodeGeneratorType> typedGenerators = new List<ITSCodeGeneratorType>();

            foreach( ITypeAttributesCache attributeCache in _attributeCache.Values )
            {
                attr = null;
                typedGenerators.Clear();

                foreach( var m in attributeCache.GetTypeCustomAttributes<ITSCodeGeneratorAutoDiscovery>() )
                {
                    if( m is ITSCodeGenerator g )
                    {
                        _globals.Add( g );
                    }
                    if( m is TypeScriptAttributeImpl a )
                    {
                        if( attr != null )
                        {
                            monitor.Error( $"Multiple TypeScriptAttribute decorate '{attributeCache.Type:N}'." );
                            _success = false;
                        }
                        attr = a;
                    }
                    if( m is ITSCodeGeneratorType tG )
                    {
                        typedGenerators.Add( tG );
                    }
                }
                if( attr != null || typedGenerators.Count > 0 )
                {
                    _typeDecorators.Add( attributeCache.Type, new TSDecoratedType( typedGenerators.ToArray(), attr?.Attribute ) );
                }
            }
            return _success;
        }

        /// <summary>
        /// Step 2: Initializes all global generators.
        /// </summary>
        bool InitializeGlobalGenerators( IActivityMonitor monitor )
        {
            bool success = true;
            foreach( var g in _globals )
            {
                success &= g.Initialize( monitor, this );
            }
            return success;
        }

        /// <summary>
        /// Step 1: Initializes the TSPocoTypeMap. All the exchangeable PocoTypes
        ///         have a corresponding TSPocoType.
        ///         During this step, the global PocoCodeGenerator checks the TypeScriptAttribute
        ///         that may decorate the IPoco and named record types and associates the appropriate
        ///         finalizer (for IPoco, IAbstractPoco and named records).
        /// </summary>
        bool RegisterAllExchangeablePocoType( IActivityMonitor monitor )
        {
            //foreach( var t in _pocoTypeSystem.AllNonNullableTypes )
            //{
            //    if( t.IsExchangeable )
            //    {
            //        _pocoTypeMap.Resol( monitor, t );
            //    }
            //}
            return true;
        }

        /// <summary>
        /// Step 2 and 3: The global ITSCodeGenerators are <see cref="ITSCodeGenerator.Initialize(IActivityMonitor, TypeScriptContext)"/>
        ///               and then <see cref="ITSCodeGenerator.GenerateCode(IActivityMonitor, TypeScriptContext)"/> is called.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="initialize">True for the first call, false for the second one.</param>
        /// <returns>True on success, false on error.</returns>
        bool CallCodeGenerators( IActivityMonitor monitor, bool initialize )
        {
            string action = initialize ? "Initializing" : "Executing";
            Debug.Assert( _success );
            // Executes all the globals.
            using( monitor.OpenInfo( $"{action} the {_globals.Count} global {nameof(ITSCodeGenerator)} TypeScript generators." ) )
            {
                foreach( var global in _globals )
                {
                    using( monitor.OpenTrace( $"{action} '{global.GetType().FullName}' global TypeScript generator." ) )
                    {
                        try
                        {
                            _success = initialize
                                        ? global.Initialize( monitor, this )
                                        : global.GenerateCode( monitor, this );
                        }
                        catch( Exception ex )
                        {
                            monitor.Error( ex );
                            _success = false;
                        }
                        if( !_success )
                        {
                            monitor.CloseGroup( "Failed." );
                            return false;
                        }
                    }
                }
            }
            return _success;
        }

    }
}
