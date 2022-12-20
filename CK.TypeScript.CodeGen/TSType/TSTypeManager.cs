using CK.Core;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Security;
using System.Threading;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Handles a map from C# types to <see cref="ITSType"/> and external <see cref="LibraryImport"/>.
    /// <para>
    /// The <see cref="object"/> type is mapped to "unknown", with no default values, no imports and no capacity to
    /// write any values by itself. To register other basic types, <see cref="RegisterStandardTypes(IActivityMonitor, TypeScriptRoot, bool, bool, bool, bool)"/>
    /// must be called.
    /// </para>
    /// </summary>
    public sealed partial class TSTypeManager
    {
        readonly Dictionary<Type, ITSType> _types;
        readonly Dictionary<string, LibraryImport> _libraries;
        readonly TypeScriptRoot _root;
        readonly IReadOnlyDictionary<string, string>? _libVersionsConfig;

        internal TSTypeManager( TypeScriptRoot root, IReadOnlyDictionary<string, string>? libraryVersionConfiguration )
        {
            _root = root;
            _libVersionsConfig = libraryVersionConfiguration;
            _libraries = new Dictionary<string, LibraryImport>();
            _types = new Dictionary<Type, ITSType>
            {
                { typeof( object ), new TSType( "unknown", null, null ) }
            };
        }

        /// <summary>
        /// Registers an imported library. The first wins: all subsequent imports with the same name will
        /// use the previously registered version and implied libraries.
        /// <para>
        /// The version defined in the configured versions is always preferred to the code
        /// specified one given by the <paramref name="version"/> parameter.
        /// When the version parameter is let to null, the version must be specified in the configured versions otherwise
        /// an ArgumentException is thrown: the code should always specify a default version.
        /// </para>
        /// <para>
        /// No check is done on the implied libraries: the first registration is left unchanged.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="name">The library name. Must not be empty or whitespace.</param>
        /// <param name="dependencyKind">The dependency kind.</param>
        /// <param name="version">The code specified version.</param>
        /// <param name="impliedDependencies">Optional dependencies that are implied by this one.</param>
        /// <returns></returns>
        public LibraryImport RegisterLibrary( IActivityMonitor monitor, string name, DependencyKind dependencyKind, string? version, params LibraryImport[] impliedDependencies )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullOrWhiteSpaceArgument( name );
            Throw.CheckArgument( version == null || !string.IsNullOrWhiteSpace( version ) );
            if( _libraries.TryGetValue( name, out var library ) )
            {
                if( version != null && version != library.Version )
                {
                    monitor.Warn( $"Library '{library.Name}' is already registered in version '{library.Version}'. Specified version '{version}' will be ignored." );
                }
                return library;
            }
            if( _libVersionsConfig?.TryGetValue( name, out version ) is true )
            {
                if( version == null || version == library.Version )
                {
                    monitor.Info( $"Library '{name}' will use the externally configured version '{version}'." );
                }
                else
                {
                    monitor.Warn( $"Library '{name}' will use the externally configured version '{version}', the code specified version '{version}' will be ignored." );
                }
            }
            if( version == null )
            {
                Throw.ArgumentException( $"The library '{name}' has no externally configured version and no version is specified by code." );
            }
            library = new LibraryImport( name, version, dependencyKind, impliedDependencies );
            _libraries.Add( name, library );
            return library;
        }

        /// <summary>
        /// Tries to find an already registered <see cref="LibraryImport"/>.
        /// </summary>
        /// <param name="name">The library name.</param>
        /// <returns>The registered import or null.</returns>
        public LibraryImport? FindRegisteredLibrary( string name )
        {
            if( _libraries.TryGetValue( name, out var libraryImport ) ) return libraryImport;
            return null;
        }

        /// <summary>
        /// Gets a registered TS type for a type or null if not found.
        /// Use the indexer <see cref="this[Type]"/> to throw if the type must be mapped.
        /// </summary>
        /// <param name="t">The C# type for which a TS type should be found.</param>
        /// <returns>The TS type or null if not found.</returns>
        public ITSType? Find( Type t ) => _types.GetValueOrDefault( t );

        /// <summary>
        /// Gets a registered TS type for a type o throws an <see cref="KeyNotFoundException"/>.
        /// </summary>
        /// <param name="t">The C# type for which a TS type should be found.</param>
        /// <returns>The TS type.</returns>
        public ITSType this[Type t] => _types[t];

        /// <summary>
        /// Registers a new C# type to <see cref="ITSType"/> mapping.
        /// This throws a <see cref="ArgumentException"/> if the type is already mapped.
        /// </summary>
        /// <param name="type">The C# type.</param>
        /// <param name="tsType">The associated TS type.</param>
        public void Register( Type type, ITSType tsType )
        {
            _types.Add( type, tsType );
        }

        /// <summary>
        /// Gets all the registered <see cref="ITSGeneratedType"/>.
        /// </summary>
        public IEnumerable<ITSGeneratedType> AllGeneratedTypes => _types.Values.OfType<ITSGeneratedType>();

        /// <summary>
        /// Event arguments that exposes a <see cref="TSGeneratedTypeBuilder"/> to be configured.
        /// </summary>
        public sealed class BuilderRequiredEventArgs : EventMonitoredArgs
        {
            readonly TSGeneratedTypeBuilder _descriptor;

            internal BuilderRequiredEventArgs( IActivityMonitor monitor, TSGeneratedTypeBuilder builder )
                : base( monitor )
            {
                _descriptor = builder;
            }

            /// <summary>
            /// Gets the type descriptor that can be configured.
            /// </summary>
            public TSGeneratedTypeBuilder Builder => _descriptor;
        }

        /// <summary>
        /// Raised when a <see cref="ITSGeneratedType"/> must be configured.
        /// </summary>
        public event EventHandler<BuilderRequiredEventArgs>? BuilderRequired;

        /// <summary>
        /// Resolves the mapping from a C# <see cref="Type"/> to a <see cref="ITSType"/>.
        /// <para>
        /// The Type may already be registered as a basic <see cref="TSType"/> and not as a <see cref="TSGeneratedType"/>:
        /// this is why this method returns the interface.
        /// </para>
        /// <para>
        /// If mo mapping exits and <see cref="IsValidGeneratedType(Type)"/> returns false, this throws
        /// an <see cref="ArgumentException"/>.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="type">The type that should be available in TypeScript.</param>
        /// <returns>The file that defines the type.</returns>
        public ITSType ResolveTSType( IActivityMonitor monitor, Type type )
        {
            Throw.CheckNotNullArgument( type );
            if( !_types.TryGetValue( type, out var mapped ) )
            {
                HashSet<Type>? cycleDetector = null;
                mapped = ResolveTSType( monitor, type, false, ref cycleDetector );
                Debug.Assert( mapped != null );
                _types.Add( type, mapped );
            }
            return mapped;
        }

        TSGeneratedType? ResolveTSType( IActivityMonitor monitor, Type type, bool internalCall, ref HashSet<Type>? cycleDetector )
        {
            if( !IsValidGeneratedType( type ) )
            {
                if( internalCall ) return null;
                Throw.ArgumentException( nameof( type ), $"Invalid type for a TSGeneratedType: '{type.ToCSharpName()}'. It must be deconstructed: array, collections, nullable value type and value tuples must be handled by the caller since these are \"inlined\" types in TypeScript." );
            }
            var d = new TSGeneratedTypeBuilder( type );
            BuilderRequired?.Invoke( this, new BuilderRequiredEventArgs( monitor, d ) );

            string typeName = d.TypeName ?? GetSafeName( type );

            TypeScriptFolder? folder = null;
            TypeScriptFile? file = null;
            Type? refTarget = d.SameFileAs ?? d.SameFolderAs;
            if( refTarget != null )
            {
                if( !_types.TryGetValue( type, out var target ) )
                {
                    if( cycleDetector == null ) cycleDetector = new HashSet<Type>();
                    if( !cycleDetector.Add( type ) ) Throw.InvalidOperationException( $"TypeScript.SameFoldeAs cycle detected: {cycleDetector.Select( c => c.Name ).Concatenate( " => " )}." );
                    target = ResolveTSType( monitor, refTarget, true, ref cycleDetector );
                }
                if( target is not ITSGeneratedType gTarget )
                {
                    monitor.Warn( $"Type '{refTarget:C}' cannot be used in SameFileAs or SameFolderAs attributes since it is not a type associated to a generated file. Type '{type:N}' will be in a folder/file based on its namespace/name." );
                }
                else
                {
                    folder = gTarget.File.Folder;
                    if( d.SameFileAs != null )
                    {
                        file = gTarget.File;
                    }
                }
            }
            if( file == null )
            {
                folder ??= _root.Root.FindOrCreateFolder( d.Folder ?? type.Namespace!.Replace( '.', '/' ) );
                file = folder.FindOrCreateFile( d.FileName ?? typeName.Replace( '<', '{' ).Replace( '>', '}' ) + ".ts" );
            }
            monitor.Trace( $"Type '{type:C}' will be generated in '{file}'.");
            return new TSGeneratedType( type, typeName, file, d.DefaultValueSource, d.TryWriteValueImplementation, d.Implementor );

            static string GetSafeName( Type t )
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

        }

        /// <summary>
        /// Gets whether a type can be used to call <see cref="DeclareTSType(IActivityMonitor, Type)"/>.
        /// <see cref="object"/>, <see cref="void"/>, arrays, collections (list, set and dictionary),
        /// nullable value types, value tuples must be handled explicitly since these are "inlined" types in TypeScript
        /// and cannot have an associated <see cref="ITSGeneratedType"/>.
        /// </summary>
        /// <param name="t">The type that may require a dedicated file.</param>
        /// <returns>True if the type can be defined by a <see cref="ITSGeneratedType"/>, false otherwise.</returns>
        public static bool IsValidGeneratedType( Type t )
        {
            if( t.IsArray )
            {
                return false;
            }
            if( t.Namespace == "System" && t.Name.StartsWith( "ValueTuple`", StringComparison.Ordinal ) )
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
            else if( t == typeof( object ) || t == typeof( void ) )
            {
                return false;
            }
            return true;
        }

        internal List<ITSGeneratedType>? GenerateCode( IActivityMonitor monitor )
        {
            List<ITSGeneratedType>? required = null;
            foreach( var type in _types.Values.OfType<TSGeneratedType>() )
            {
                var g = type._codeGenerator;
                if( g == null )
                {
                    monitor.Warn( $"The type '{type.Type:C}' has no TypeScript implementor function." );
                    required ??= new List<ITSGeneratedType>();
                    required.Add( type );
                }
                else if( !g( monitor, type ) )
                {
                    monitor.Error( $"TypeScript implementor for type '{type.Type:C}' failed." );
                }
                else if( type.TypePart == null )
                {
                    monitor.Warn( $"TypeScript implementor for type '{type.Type:C}' didn't create the TypePart." );
                    required ??= new List<ITSGeneratedType>();
                    required.Add( type );
                }
            }
            return required;
        }
    }

}

