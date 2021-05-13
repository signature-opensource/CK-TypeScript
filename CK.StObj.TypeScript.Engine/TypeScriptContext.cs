using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using CK.Text;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace CK.Setup
{
    /// <summary>
    /// Central class that handles TypeScript generation in a <see cref="TypeScriptRoot"/> and <see cref="ICodeGenerationContext"/>.
    /// This is instantiated and made available to the participants (<see cref="ITSCodeGenerator"/> and <see cref="ITSCodeGeneratorType"/>)
    /// only if the configuration actually allows the TypeScript generation for this <see cref="CodeContext"/>.
    /// </summary>
    public class TypeScriptContext
    {
        readonly Dictionary<Type, TSTypeFile?> _typeMappings;
        readonly IReadOnlyDictionary<Type, ITypeAttributesCache> _attributeCache;
        readonly List<TSTypeFile> _typeFiles;
        IReadOnlyList<ITSCodeGenerator> _globals;
        bool _success;

        internal TypeScriptContext( TypeScriptRoot root, ICodeGenerationContext codeCtx )
        {
            Root = root;
            CodeContext = codeCtx;
            _typeMappings = new Dictionary<Type, TSTypeFile?>();
            _attributeCache = codeCtx.CurrentRun.EngineMap.AllTypesAttributesCache;
            _typeFiles = new List<TSTypeFile>();
            _success = true;
        }

        /// <summary>
        /// Gets the TypeScript code generation context.
        /// </summary>
        public TypeScriptRoot Root { get; }

        /// <summary>
        /// Gets the <see cref="ICodeGenerationContext"/> that is being processed.
        /// </summary>
        public ICodeGenerationContext CodeContext { get; }

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
        /// Declares the required support of a type.
        /// This may create one <see cref="TSTypeFile"/> for this type (if it is not an intrinsic type, see remarks)
        /// and others for generic parameters.
        /// All declared types that requires a file should eventually be generated.
        /// </summary>
        /// <remarks>
        /// "Intrinsic types" are boolean, int, string, float double, object but also array, list, set, dictionary and value tuple.
        /// They can be written directly in TypeScript and don't require a dedicated file (but note that they may reference one or
        /// more other types).
        /// </remarks>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="t">The type that should be available in TypeScript.</param>
        /// <param name="requiresFile">True to emit an error log if the returned file is null.</param>
        /// <returns>The mapped file or null if <paramref name="t"/> doesn't need a file or cannot be mapped.</returns>
        public TSTypeFile? DeclareTSType( IActivityMonitor monitor, Type t, bool requiresFile = false )
        {
            if( t == null ) throw new ArgumentNullException( nameof( t ) );
            HashSet<Type>? _ = null;
            var f = DeclareTSType( monitor, t, ref _ );
            if( f == null && requiresFile )
            {
                monitor.Error( $"Unable to obtain a TypeScript mapping for type '{t}'." );
            }
            return f;
        }

        /// <summary>
        /// Declares the required support of any number of types.
        /// This may create any number of <see cref="TSTypeFile"/> that should eventually be generated.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="types">The types that should be available in TypeScript.</param>
        public void DeclareTSType( IActivityMonitor monitor, IEnumerable<Type> types )
        {
            foreach( var t in types )
                if( t != null )
                    DeclareTSType( monitor, t );
        }

        /// <summary>
        /// Declares the required support of any number of types.
        /// This may create any number of <see cref="TSTypeFile"/> that should eventually be generated.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="types">The types that should be available in TypeScript.</param>
        public void DeclareTSType( IActivityMonitor monitor, params Type[] types ) => DeclareTSType( monitor, (IEnumerable<Type>)types );

        TSTypeFile? DeclareTSType( IActivityMonitor monitor, Type t, ref HashSet<Type>? cycleDetector )
        {
            TSTypeFile CreateNoCacheAttributeTSFile( IActivityMonitor monitor, Type t, ref HashSet<Type>? cycleDetector )
            {
                var attribs = t.GetCustomAttributes( typeof( TypeScriptAttribute ), false );
                var attr = attribs.Length == 1 ? (TypeScriptAttribute)attribs[0] : null;
                var f = new TSTypeFile( this, t, Array.Empty<ITSCodeGeneratorType>(), attr );
                _typeMappings.Add( t, f );
                _typeFiles.Add( f );
                return f;
            }

            if( !_typeMappings.TryGetValue( t, out var f ) )
            {
                if( t.IsArray )
                {
                    DeclareTSType( monitor, t.GetElementType()! );
                }
                else if( t.IsValueTuple() )
                {
                    foreach( var s in t.GetGenericArguments() )
                    {
                        DeclareTSType( monitor, s );
                    }
                }
                else if( t.IsGenericType )
                {
                    Type tDef;
                    if( t.IsGenericTypeDefinition )
                    {
                        tDef = t;
                    }
                    else
                    {
                        foreach( var a in t.GetGenericArguments() )
                        {
                            DeclareTSType( monitor, a );
                        }
                        tDef = t.GetGenericTypeDefinition();
                    }
                    if( tDef != typeof( IDictionary<,> )
                        && tDef != typeof( Dictionary<,> )
                        && tDef != typeof( ISet<> )
                        && tDef != typeof( HashSet<> )
                        && tDef != typeof( IList<> )
                        && tDef != typeof( List<> ) )
                    {
                        f = CreateNoCacheAttributeTSFile( monitor, tDef, ref cycleDetector );
                    }
                }
                else if( t != typeof( int )
                         && t != typeof( float )
                         && t != typeof( double )
                         && t != typeof( bool )
                         && t != typeof( string )
                         && t != typeof( object ) )
                {
                    f = CreateNoCacheAttributeTSFile( monitor, t, ref cycleDetector );
                }
            }

            // We always check the initialization to handle TSTypeFile created by
            // the attributes.
            if( f != null && !f.IsInitialized )
            {
                EnsureInitialized( monitor, f, ref cycleDetector );
            }
            return f;
        }
        TSTypeFile EnsureInitialized( IActivityMonitor monitor, TSTypeFile f, ref HashSet<Type>? cycleDetector )
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
                if( !cycleDetector.Add( t ) ) throw new InvalidOperationException( $"TypeScript.SameFoldeAs cycle detected: {cycleDetector.Select( c => c.Name ).Concatenate( " => " )}." );

                var target = DeclareTSType( monitor, refTarget, ref cycleDetector );
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
            var defName = SafeNameChars( t.GetExternalName() ?? GetSafeName( t ) );
            fileName ??= attr.FileName ?? (SafeFileChars( defName ) + ".ts");
            string typeName = attr.TypeName ?? defName;
            f.Initialize( folder, fileName, typeName );
            monitor.Trace( f.ToString() );
            return f;
        }

        internal static string GetPocoClassNameFromPrimaryInterface( IPocoRootInfo itf )
        {
            var typeName = itf.PrimaryInterface.GetExternalName() ?? TypeScriptContext.GetSafeName( itf.PrimaryInterface );
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
            return BuildTSTypeFilesFromAttributesAndDiscoverGenerators( monitor )
                   && CallCodeGenerators( monitor, true )
                   && CallCodeGenerators( monitor, false )
                   && EnsureTypesGeneration( monitor );
        }

        bool BuildTSTypeFilesFromAttributesAndDiscoverGenerators( IActivityMonitor monitor )
        {
            var globals = new List<ITSCodeGenerator>();
            globals.Add( new TSIPocoCodeGenerator( CodeContext.CurrentRun.ServiceContainer.GetService<IPocoSupportResult>( true ) ) );

            // Reused per type.
            TypeScriptAttributeImpl? impl = null;
            List<ITSCodeGeneratorType> generators = new List<ITSCodeGeneratorType>();

            foreach( ITypeAttributesCache attributeCache in _attributeCache.Values )
            {
                impl = null;
                generators.Clear();

                foreach( var m in attributeCache.GetTypeCustomAttributes<ITSCodeGeneratorAutoDiscovery>() )
                {
                    if( m is ITSCodeGenerator g )
                    {
                        globals.Add( g );
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
            _globals = globals;
            return _success;
        }

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

        bool EnsureTypesGeneration( IActivityMonitor monitor )
        {
            using( monitor.OpenInfo( $"Ensuring that {_typeFiles.Count} types are initialized and implemented." ) )
            {
                for( int i = 0; i < _typeFiles.Count; ++i )
                {
                    var f = _typeFiles[i];
                    if( !f.IsInitialized )
                    {
                        HashSet<Type>? _ = null;
                        EnsureInitialized( monitor, f, ref _ );
                    }
                    _success &= f.Implement( monitor );
                }
            }
            return _success;
        }

    }
}
