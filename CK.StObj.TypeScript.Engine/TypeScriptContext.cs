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
        readonly Dictionary<Type, TSTypeFile?> _typeMappings;
        readonly IReadOnlyDictionary<Type, ITypeAttributesCache> _attributeCache;
        readonly List<TSTypeFile> _typeFiles;
        readonly IPocoTypeSystem _pocoTypeSystem;
        readonly TSPocoTypeMap _pocoTypeMap;
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
            _typeMappings = new Dictionary<Type, TSTypeFile?>();
            _attributeCache = codeCtx.CurrentRun.EngineMap.AllTypesAttributesCache;
            _typeFiles = new List<TSTypeFile>();
            _pocoTypeMap = new TSPocoTypeMap( this, pocoTypeSystem );
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

        /// <summary>
        /// Gets a <see cref="TSTypeFile"/> for a type if it has been declared so far and is not
        /// an intrinsic types (see remarks of <see cref="DeclareTSType(IActivityMonitor, Type, bool)"/>).
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>The Type to File association if is has been declared.</returns>
        public TSTypeFile? FindDeclaredTSType( Type t ) => _typeMappings.GetValueOrDefault( t );

        /// <summary>
        /// Declares a file to define the required support of a type.
        /// All files should eventually be generated.
        /// <para>
        /// If <see cref="IsValidTypeForTSTypeFile(Type)"/> returns false, this throws an <see cref="ArgumentException"/>.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="t">The type that should be available in TypeScript.</param>
        /// <returns>The file that defines the type.</returns>
        public TSTypeFile DeclareTSType( IActivityMonitor monitor, Type t )
        {
            Throw.CheckNotNullArgument( t );
            HashSet<Type>? _ = null;
            return DeclareTSType( monitor, t, true, ref _ )!;
        }

        /// <summary>
        /// Gets whether a type can be used to call <see cref="DeclareTSType(IActivityMonitor, Type)"/>.
        /// Arrays, collections (list, set and dictionary), nullable value types, value tuples must be
        /// handled explicitly since these are "inlined" types in TypeScript and cannot have an associated
        /// <see cref="TSTypeFile"/>.
        /// </summary>
        /// <param name="t">The type that may require a dedicated file.</param>
        /// <returns>True if the type can be defined in a <see cref="TSTypeFile"/>, false otherwise.</returns>
        public static bool IsValidTypeForTSTypeFile( Type t )
        {
            if( t.IsArray )
            {
                return false;
            }
            if( t.IsValueTuple() )
            {
                return false;
            }
            if( t.IsGenericType )
            {
                Type tDef = t.IsGenericTypeDefinition ? t : t.GetGenericTypeDefinition();
                if( tDef == typeof( Nullable<> )
                    || tDef == typeof( IDictionary<,> )
                    || tDef == typeof( Dictionary<,> )
                    || tDef == typeof( ISet<> )
                    || tDef == typeof( HashSet<> )
                    || tDef == typeof( IList<> )
                    || tDef == typeof( List<> ) )
                {
                    return false;
                }
            }
            else if( IntrinsicTypeName( t ) != null )
            {
                return false;
            }
            return true;
        }

        TSTypeFile? DeclareTSType( IActivityMonitor monitor, Type type, bool requiresFile, ref HashSet<Type>? cycleDetector )
        {
            Throw.CheckNotNullArgument( type );
            if( !_typeMappings.TryGetValue( type, out var f ) )
            {
                if( IsValidTypeForTSTypeFile( type ) )
                {
                    var attribs = type.GetCustomAttributes( typeof( TypeScriptAttribute ), false );
                    var attr = attribs.Length == 1 ? (TypeScriptAttribute)attribs[0] : null;
                    f = new TSTypeFile( this, type, Array.Empty<ITSCodeGeneratorType>(), attr );
                    _typeFiles.Add( f );
                }
                _typeMappings.Add( type, f );
            }
            if( f == null )
            {
                if( requiresFile )
                {
                    Throw.ArgumentException( nameof( type ), $"Invalid type for TSTypeFile: '{type.ToCSharpName()}'. It must be deconstructed: array, collections, nullable value type and value tuples must be handled by the caller since these are \"inlined\" types in TypeScript." );
                }
            }
            else
            {
                // We always check the initialization to handle TSTypeFile created by
                // the attributes: the first call to DeclareTSType will initialize them.
                if( f != null && !f.IsInitialized )
                {
                    EnsureTSFileInitialization( monitor, f, ref cycleDetector );
                }
            }
            return f;

        }

        TSTypeFile EnsureTSFileInitialization( IActivityMonitor monitor, TSTypeFile f, ref HashSet<Type>? cycleDetector )
        {
            Debug.Assert( !f.IsInitialized );
            TypeScriptAttribute attr = f.Attribute;
            var generators = f.Generators;
            var t = f.Type;

            foreach( var g in _globals )
            {
                _success &= g.ConfigureTypeScriptAttribute( monitor, f, attr );
            }
            if( generators.Count > 0 )
            {
                foreach( var g in generators )
                {
                    _success &= g.ConfigureTypeScriptAttribute( monitor, f, attr );
                }
            }

            NormalizedPath folder;
            string? fileName = null;
            Type? refTarget = attr.SameFileAs ?? attr.SameFolderAs;
            if( refTarget != null )
            {
                if( cycleDetector == null ) cycleDetector = new HashSet<Type>();
                if( !cycleDetector.Add( t ) ) Throw.InvalidOperationException( $"TypeScript.SameFoldeAs cycle detected: {cycleDetector.Select( c => c.Name ).Concatenate( " => " )}." );

                var target = DeclareTSType( monitor, refTarget, false, ref cycleDetector );
                if( target == null )
                {
                    monitor.Error( $"Type '{refTarget}' cannot be used in SameFileAs or SameFolderAs attributes." );
                    _success = false;
                    folder = default;
                }
                else
                {
                    folder = target.Folder;
                    if( attr.SameFileAs != null )
                    {
                        fileName = target.FileName;
                    }
                }
            }
            else
            {
                folder = attr.Folder ?? t.Namespace!.Replace( '.', '/' );
            }
            string typeName = attr.TypeName ?? SafeNameChars( t.GetExternalName() ?? GetSafeName( t ) );

            fileName ??= attr.FileName ?? (SafeFileChars( typeName ) + ".ts");
            f.Initialize( folder, fileName, typeName );
            monitor.Trace( f.ToString() );
            return f;
        }

        internal static string GetPocoClassNameFromExternalOrCSharpName( IPrimaryPocoType p )
        {
            var typeName = p.ExternalOrCSharpName;
            var iDot = typeName.LastIndexOf( '.' );
            if( iDot >= 0 ) typeName = typeName.Substring( iDot );
            typeName = TypeScriptContext.SafeNameChars( typeName );
            if( typeName.StartsWith( "I" ) ) typeName = typeName.Remove( 0, 1 );
            return typeName;
        }

        internal static string SafeNameChars( string name )
        {
            return name.Replace( '+', '_' );
        }
        internal static string SafeFileChars( string name )
        {
            return name.Replace( '<', '{' ).Replace( '>', '}' );
        }
        internal static string GetSafeName( Type t )
        {
            var n = t.Name;
            if( !t.IsGenericType )
            {
                return n;
            }
            Type tDef = t.IsGenericTypeDefinition ? t : t.GetGenericTypeDefinition();
            n += '<' + tDef.GetGenericArguments().Select( a => a.Name ).Concatenate() + '>';
            return n;
        }

        internal bool Run( IActivityMonitor monitor )
        {
            using( monitor.OpenInfo( "Running TypeScript code generation." ) )
            {
                return BuildTSTypeFilesFromAttributesAndDiscoverGenerators( monitor )
                       && RegisterAllExchangeablePocoType( monitor )
                       && CallCodeGenerators( monitor, initialize: true )
                       && CallCodeGenerators( monitor, false )
                       && EnsureTypesGeneration( monitor );
            }
        }

        /// <summary>
        /// Step 0: Discovering the generators and TypeScript attributes thanks to ITSCodeGeneratorAutoDiscovery.
        ///         Registers the globals and Type bound generators.
        /// </summary>
        bool BuildTSTypeFilesFromAttributesAndDiscoverGenerators( IActivityMonitor monitor )
        {
            _globals.Add( new PocoCodeGenerator( _pocoTypeSystem, _pocoTypeMap ) );

            // These variables are reused per type.
            TypeScriptAttributeImpl? impl;
            List<ITSCodeGeneratorType> generators = new List<ITSCodeGeneratorType>();

            foreach( ITypeAttributesCache attributeCache in _attributeCache.Values )
            {
                impl = null;
                generators.Clear();

                foreach( var m in attributeCache.GetTypeCustomAttributes<ITSCodeGeneratorAutoDiscovery>() )
                {
                    if( m is ITSCodeGenerator g )
                    {
                        _globals.Add( g );
                    }
                    if( m is TypeScriptAttributeImpl a )
                    {
                        if( impl != null )
                        {
                            monitor.Error( $"Multiple TypeScriptImpl decorates '{attributeCache.Type}'." );
                            _success = false;
                        }
                        impl = a;
                    }
                    if( m is ITSCodeGeneratorType tG )
                    {
                        generators.Add( tG );
                    }
                }
                if( impl != null || generators.Count > 0 )
                {
                    var f = new TSTypeFile( this, attributeCache.Type, generators.ToArray(), impl?.Attribute );
                    _typeMappings.Add( attributeCache.Type, f );
                    _typeFiles.Add( f );
                }
            }
            return _success;
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
            foreach( var t in _pocoTypeSystem.AllNonNullableTypes )
            {
                if( t.IsExchangeable )
                {
                    _pocoTypeMap.GetTSPocoType( monitor, t );
                }
            }
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

        /// <summary>
        /// Step 4: Ensures that all the TSTypeFile that have been declared (<see cref="DeclareTSType(IActivityMonitor, Type)"/>)
        ///         are initialized and then calls their internal <see cref="TSTypeFile.Implement(IActivityMonitor)"/> that
        ///         runs all its <see cref="TSTypeFile.Generators"/> and its <see cref="TSTypeFile.Finalizer"/>.
        /// </summary>
        bool EnsureTypesGeneration( IActivityMonitor monitor )
        {
            using( monitor.OpenInfo( $"Ensuring that {_typeFiles.Count} types are initialized and implemented." ) )
            {
                for( int i = 0; i < _typeFiles.Count; ++i )
                {
                    var f = _typeFiles[i];
                    if( !f.IsInitialized )
                    {
                        HashSet<Type>? unusedCycleDetector = null;
                        EnsureTSFileInitialization( monitor, f, ref unusedCycleDetector );
                    }
                    _success &= f.Implement( monitor );
                }
            }
            return _success;
        }

    }
}
