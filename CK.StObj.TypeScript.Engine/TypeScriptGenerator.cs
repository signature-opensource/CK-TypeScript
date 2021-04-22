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
    /// Central class allows TypeScript generation in a <see cref="ICodeGenerationContext"/>.
    /// This is made available to the participants (<see cref="ITSCodeGenerator"/> and <see cref="ITSCodeGeneratorType"/>)
    /// only if the configuration actually allows the TypeScript generation for this <see cref="CodeContext"/>.
    /// </summary>

    public class TypeScriptGenerator
    {
        readonly Dictionary<Type, TSTypeFile> _typeMappings;
        readonly IReadOnlyDictionary<Type,ITypeAttributesCache> _attributeCache;
        readonly List<TSTypeFile> _typeFiles;
        IReadOnlyList<ITSCodeGenerator> _globals;
        bool _success;

        internal TypeScriptGenerator( TypeScriptCodeGenerationContext tsCtx, ICodeGenerationContext codeCtx )
        {
            Context = tsCtx;
            CodeContext = codeCtx;
            _typeMappings = new Dictionary<Type, TSTypeFile>();
            _attributeCache = codeCtx.CurrentRun.EngineMap.AllTypesAttributesCache;
            _typeFiles = new List<TSTypeFile>();
            _success = true;
        }


        /// <summary>
        /// Gets the TypeScript code generation context.
        /// </summary>
        public TypeScriptCodeGenerationContext Context { get; }

        /// <summary>
        /// Gets the <see cref="ICodeGenerationContext"/> that is being processed.
        /// </summary>
        public ICodeGenerationContext CodeContext { get; }

        /// <summary>
        /// Gets all the global generators.
        /// </summary>
        public IReadOnlyList<ITSCodeGenerator> GlobalGenerators => _globals;

        /// <summary>
        /// Gets the <see cref="TSTypeFile"/> for a type.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="t">The type.</param>
        /// <returns>The type file (<see cref="TSTypeFile.File"/> may not yet exists).</returns>
        public TSTypeFile GetTSTypeFile( IActivityMonitor monitor, Type t )
        {
            HashSet<Type>? _ = null;
            return DoGetTSTypeFile( monitor, t, ref _ );
        }

        /// <summary>
        /// Declares the <see cref="TSTypeFile"/> for a set of types: the TypeScript code for
        /// these types must eventually be generated.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="types">The types for which TypeScript must be generated.</param>
        public void DeclareTSType( IActivityMonitor monitor, IEnumerable<Type> types )
        {
            foreach( var t in types ) GetTSTypeFile( monitor, t );
        }

        /// <summary>
        /// Declares the <see cref="TSTypeFile"/> for a set of types: the TypeScript code for
        /// these types must eventually be generated.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="t">One or more types for which TypeScript must be generated.</param>
        public void DeclareTSType( IActivityMonitor monitor, params Type[] t ) => DeclareTSType( monitor, (IEnumerable<Type>)t );

        /// <summary>
        /// Gets a identifier that follows <see cref="TypeScriptCodeGenerationContext.PascalCase"/> configuration.
        /// Only the first character is handled.
        /// </summary>
        /// <param name="s">The identifier.</param>
        /// <returns>A formatted identifier.</returns>
        public string ToIdentifier( string s )
        {
            if( s.Length != 0  && Char.IsUpper( s, 0 ) != Context.PascalCase )
            {
                return Context.PascalCase
                        ? (s.Length == 1
                            ? s.ToUpperInvariant()
                            : Char.ToUpperInvariant( s[0] ) + s.Substring( 1 ))
                        : (s.Length == 1
                            ? s.ToLowerInvariant()
                            : Char.ToLowerInvariant( s[0] ) + s.Substring( 1 ));
            }
            return s;
        }


        internal bool BuildTSTypeFilesFromAttributes( IActivityMonitor monitor )
        {
            var globals = new List<ITSCodeGenerator>();
            globals.Add( new TypeScriptIPocoGenerator( CodeContext.CurrentRun.ServiceContainer.GetService<IPocoSupportResult>( true ) ) );

            // Reused per type.
            TypeScriptImpl? impl = null;
            List<ITSCodeGeneratorType> generators = new List<ITSCodeGeneratorType>();

            foreach( var attributeCache in _attributeCache.Values )
            {
                impl = null;
                generators.Clear();

                foreach( var m in attributeCache.GetTypeCustomAttributes<ITSCodeGeneratorAutoDiscovery>() )
                {
                    if( m is ITSCodeGenerator g )
                    {
                        globals.Add( g );
                    }
                    if( m is TypeScriptImpl a ) impl = a;
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

        TSTypeFile DoGetTSTypeFile( IActivityMonitor monitor, Type t, ref HashSet<Type>? cycleDetector )
        {
            TSTypeFile? f = _typeMappings.GetValueOrDefault( t );
            if( f == null )
            {
                Debug.Assert( !_attributeCache.ContainsKey( t ) );
                f = new TSTypeFile( this, t, Array.Empty<ITSCodeGeneratorType>(), null );
                _typeMappings.Add( t, f );
                _typeFiles.Add( f );
            }
            if( !f.IsInitialized ) EnsureInitialized( monitor, f, ref cycleDetector );
            return f;
        }

        TSTypeFile EnsureInitialized( IActivityMonitor monitor, TSTypeFile f, ref HashSet<Type>? cycleDetector )
        {
            Debug.Assert( !f.IsInitialized );
            TypeScriptAttribute attr = f.Attribute;
            var generators = f.Generators;
            var t = f.Type;

            Func<IActivityMonitor, TSTypeFile, bool>? finalizer = null;
            foreach( var g in _globals )
            {
                _success &= g.ConfigureTypeScriptAttribute( monitor, this, t, attr, f.Generators, ref finalizer );
            }
            if( generators.Count > 0 )
            {
                foreach( var g in generators )
                {
                    _success &= g.ConfigureTypeScriptAttribute( monitor, attr, (IReadOnlyList<ITSCodeGeneratorType>)f.Generators, ref finalizer );
                }
            }

            NormalizedPath folder;
            string? fileName = null;
            Type? refTarget = attr.SameFileAs ?? attr.SameFolderAs;
            if( refTarget != null )
            {
                if( cycleDetector == null ) cycleDetector = new HashSet<Type>();
                if( !cycleDetector.Add( t ) ) throw new InvalidOperationException( $"TypeScript.SameFoldeAs cycle detected: {cycleDetector.Select( c => c.Name ).Concatenate( " => " )}." );

                var target = DoGetTSTypeFile( monitor, refTarget, ref cycleDetector );
                folder = target.Folder;
                if( attr.SameFileAs != null )
                {
                    fileName = target.FileName;
                }
            }
            else
            {
                folder = attr.Folder ?? t.Namespace!.Replace( '.', '/' );
            }
            var defName = t.GetExternalName() ?? t.Name;
            fileName ??= attr.FileName ?? (defName + ".ts");
            string typeName = attr.TypeName ?? defName;
            f.Initialize( folder, fileName, typeName, finalizer );
            monitor.Trace( f.ToString() );
            return f;
        }

        internal bool CallCodeGenerators( IActivityMonitor monitor )
        {
            Debug.Assert( _success );
            // Executes all the globals.
            using( monitor.OpenInfo( $"Executing the {_globals.Count} global {nameof(ITSCodeGenerator)} TypeScript generators." ) )
            {
                foreach( var global in _globals )
                {
                    using( monitor.OpenTrace( $"Executing '{global.GetType().FullName}' global TypeScript generator." ) )
                    {
                        if( !global.GenerateCode( monitor, this ) )
                        {
                            monitor.CloseGroup( "Failed." );
                            return _success = false;
                        }
                    }
                }
            }
            //
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
