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
    /// Handles a map from "type key" to <see cref="ITSType"/>. The type key can be a C# type but may be another object.
    /// <para>
    /// A <see cref="Type"/> key is enough for value types since the non nullable type and the nullable type can be registered, this is
    /// what <see cref="RegisterStandardTypes(IActivityMonitor, bool, bool, bool, bool)"/> does. To be able to handle reference type nullability,
    /// the key can be any object.
    /// </para>
    /// <para>
    /// The <see cref="object"/> type is mapped to "unknown", with no default values, no imports and no capacity to
    /// write any values by itself. To register other basic types, <see cref="RegisterStandardTypes(IActivityMonitor, bool, bool, bool, bool)"/>
    /// must be called.
    /// </para>
    /// </summary>
    public sealed partial class TSTypeManager
    {
        // Null value is used to detect reentrancy while resolving.
        readonly Dictionary<object, ITSType?> _types;
        readonly Dictionary<string, LibraryImport> _libraries;
        readonly TypeScriptRoot _root;
        readonly IReadOnlyDictionary<string, string>? _libVersionsConfig;
        // New TSGeneratedType are appended to this list: GenerateCode
        // loops until no new type appears in this list.
        readonly List<TSGeneratedType> _processList;

        internal TSTypeManager( TypeScriptRoot root, IReadOnlyDictionary<string, string>? libraryVersionConfiguration )
        {
            _root = root;
            _libVersionsConfig = libraryVersionConfiguration;
            _libraries = new Dictionary<string, LibraryImport>();
            _types = new Dictionary<object, ITSType?>
            {
                { typeof( object ), new TSType( "unknown", null, null ) }
            };
            _processList = new List<TSGeneratedType>();
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
        /// Gets a registered TS type for a type key or null if not found.
        /// Use the indexer <see cref="this[object]"/> to throw if the type must be mapped.
        /// </summary>
        /// <param name="keyType">The key type for which a TS type should be found.</param>
        /// <returns>The TS type or null if not found.</returns>
        public ITSType? Find( object keyType ) => _types.GetValueOrDefault( keyType );

        /// <summary>
        /// Gets a registered TS type for a type o throws an <see cref="KeyNotFoundException"/>.
        /// </summary>
        /// <param name="keyType">The key type for which a TS type should be found.</param>
        /// <returns>The TS type.</returns>
        public ITSType this[object keyType] => _types[keyType] ?? throw new KeyNotFoundException( $"Key type '{keyType}' is currently resolving." );

        /// <summary>
        /// Registers a new key type to <see cref="ITSType"/> mapping.
        /// This throws a <see cref="ArgumentException"/> if the key is already mapped.
        /// </summary>
        /// <param name="keyType">The key type.</param>
        /// <param name="tsType">The associated TS type.</param>
        public void Register( object keyType, ITSType tsType )
        {
            _types.Add( keyType, tsType );
        }

        /// <summary>
        /// Registers a value type by its type and its nullable type (to <see cref="ITSType.Nullable"/>).
        /// </summary>
        /// <typeparam name="T">The value type to register.</typeparam>
        /// <param name="tsType">The associated TypeScript type. Must be the non nullable type.</param>
        public void RegisterValueType<T>( ITSType tsType ) where T : struct
        {
            Throw.CheckNotNullArgument( tsType );
            Throw.CheckArgument( !tsType.IsNullable );
            _types.Add( typeof( T ), tsType );
            _types.Add( typeof( T? ), tsType.Nullable );
        }

        /// <summary>
        /// Event arguments that exposes a <see cref="TSGeneratedTypeBuilder"/> to be configured.
        /// This is raised when a C# type must be resolved.
        /// </summary>
        public sealed class TypeBuilderRequiredEventArgs : EventMonitoredArgs
        {
            readonly TSGeneratedTypeBuilder _descriptor;

            internal TypeBuilderRequiredEventArgs( IActivityMonitor monitor, TSGeneratedTypeBuilder builder )
                : base( monitor )
            {
                _descriptor = builder;
            }

            /// <summary>
            /// Gets the type builder that can be configured.
            /// </summary>
            public TSGeneratedTypeBuilder Builder => _descriptor;
        }

        /// <summary>
        /// Event arguments that exposes an object for which a <see cref="ITSType"/> must be resolved.
        /// This is raised when a key type that is not a C# type must be resolved.
        /// </summary>
        public sealed class TSTypeRequiredEventArgs : EventMonitoredArgs
        {
            readonly object _keyType;
            ITSType? _resolved;

            internal TSTypeRequiredEventArgs( IActivityMonitor monitor, object keyType )
                : base( monitor )
            {
                _keyType = keyType;
            }

            /// <summary>
            /// Gets the key type to resolve.
            /// </summary>
            public object KeyType => _keyType;

            /// <summary>
            /// Gets the TypeScript type to used.
            /// </summary>
            public ITSType? Resolved
            {
                get => _resolved;
                set => _resolved = value;
            }
        }

        /// <summary>
        /// Raised when a <see cref="ITSGeneratedType"/> must be configured from a C# type.
        /// </summary>
        public event EventHandler<TypeBuilderRequiredEventArgs>? TypeBuilderRequired;

        /// <summary>
        /// Raised when a <see cref="ITSType"/> must be resolved for an object key type
        /// that is not a C# type.
        /// </summary>
        public event EventHandler<TSTypeRequiredEventArgs>? TSTypeRequired;

        /// <summary>
        /// Resolves the mapping from a key type to a <see cref="ITSType"/> by raising <see cref="TypeBuilderRequired"/> events.
        /// <para>
        /// The Type may already be registered as a basic <see cref="TSType"/> and not as a <see cref="TSGeneratedType"/>:
        /// this is why this method returns the interface.
        /// </para>
        /// <para>
        /// If no mapping exits and <see cref="IsValidGeneratedType(Type)"/> returns false, this throws
        /// an <see cref="ArgumentException"/>.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="keyType">The type that should be available in TypeScript.</param>
        /// <returns>The file that defines the type.</returns>
        public ITSType ResolveTSType( IActivityMonitor monitor, object keyType )
        {
            Throw.CheckNotNullArgument( keyType );
            if( !_types.TryGetValue( keyType, out var mapped ) )
            {
                if( keyType is Type t )
                {
                    HashSet<Type>? cycleSameFolderDetector = null;
                    mapped = ResolveTSTypeFromType( monitor, t, false, ref cycleSameFolderDetector );
                }
                else
                {
                    mapped = ResolveTSTypeFromObject( monitor, keyType );
                }
                Throw.DebugAssert( mapped != null );
                _types.Add( keyType, mapped );
            }
            if( mapped == null )
            {
                // We cannot order the items here but we don't really care.
                var cycle = _types.Where( kv => kv.Value == null ).Select( kv => kv.Key is Type t ? t.Name : kv.Key.ToString() ).Concatenate( "', '" );
                Throw.CKException( $"Reentrant TSType resolution detected: '{cycle}'." );
            }
            return mapped;
        }

        ITSType ResolveTSTypeFromObject( IActivityMonitor monitor, object keyType )
        {
            var e = new TSTypeRequiredEventArgs( monitor, keyType );
            TSTypeRequired?.Invoke( this, e );
            if( e.Resolved == null )
            {
                Throw.CKException( $"Unable to resolve TSType from keyType '{keyType}'." );
            }
            return e.Resolved;
        }

        ITSType? ResolveTSTypeFromType( IActivityMonitor monitor, Type t, bool internalCall, ref HashSet<Type>? sameFolderDetector )
        {
            if( !IsValidGeneratedType( t ) )
            {
                if( internalCall ) return null;
                Throw.ArgumentException( nameof( t ), $"Invalid type for a TSGeneratedType: '{t.ToCSharpName()}'. It must be deconstructed: array, collections, value tuples and nullable value types must be handled by the caller since these are \"inlined\" types in TypeScript." );
            }
            var d = new TSGeneratedTypeBuilder( t );
            TypeBuilderRequired?.Invoke( this, new TypeBuilderRequiredEventArgs( monitor, d ) );

            string typeName = d.TypeName ?? GetSafeName( t );

            TypeScriptFolder? folder = null;
            TypeScriptFile? file = null;
            Type? refTarget = d.SameFileAs ?? d.SameFolderAs;
            if( refTarget != null )
            {
                if( !_types.TryGetValue( t, out var target ) )
                {
                    sameFolderDetector ??= new HashSet<Type>();
                    if( !sameFolderDetector.Add( t ) )
                    {
                        Throw.InvalidOperationException( $"TypeScript.SameFoldeAs cycle detected: {sameFolderDetector.Select( c => c.Name ).Concatenate( " => " )}." );
                    }
                    target = ResolveTSTypeFromType( monitor, refTarget, true, ref sameFolderDetector );
                }
                if( target is not ITSGeneratedType gTarget )
                {
                    monitor.Warn( $"Type '{refTarget:C}' cannot be used in SameFileAs or SameFolderAs attributes since it is not a type associated to a generated file. Type '{t:N}' will be in a folder/file based on its namespace/name." );
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
                folder ??= _root.Root.FindOrCreateFolder( d.Folder ?? t.Namespace!.Replace( '.', '/' ) );
                file = folder.FindOrCreateFile( d.FileName ?? typeName.Replace( '<', '{' ).Replace( '>', '}' ) + ".ts" );
            }
            monitor.Trace( $"Type '{t:C}' will be generated in '{file}'." );
            if( t.IsEnum )
            {
                d.DefaultValueSource ??= SelectEnumTypeDefaultValue( monitor, d );
            }
            var newOne = new TSGeneratedType( t, typeName, file, d.DefaultValueSource, d.TryWriteValueImplementation, d.Implementor );
            _processList.Add( newOne );
            return newOne;

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

        static string? SelectEnumTypeDefaultValue( IActivityMonitor monitor, TSGeneratedTypeBuilder d )
        {
            Debug.Assert( d.Type.IsEnum && d.DefaultValueSource == null );
            // [Doc] The elements of the array are sorted by the binary values (that is, the unsigned values)
            //       of the enumeration constants.
            // 
            // => This is perfect for us: if 0 is defined (that is the "normal" default), then it will be
            //    the first value even if negative exist.
            Array values = d.Type.GetEnumValues();
            if( values.Length == 0 )
            {
                monitor.Warn( $"Enum '{d.TypeName}' is empty. A default value cannot be synthesized." );
            }
            else
            {
                object? value = values.GetValue( 0 );
                string? defaultValueName;
                if( value == null || (defaultValueName = d.Type.GetEnumName( value )) == null )
                {
                    monitor.Warn( $"Enum '{d.TypeName}' has a null first value or name for the first value. A default value cannot be synthesized." );
                }
                else
                {
                    monitor.Info( $"Enum '{d.TypeName}', default value selected is '{defaultValueName} = {value:D}'." );
                    return $"{d.TypeName}.{defaultValueName}";
                }
            }
            return null;
        }

        /// <summary>
        /// Gets whether a type can be used to call <see cref="ResolveTSType(IActivityMonitor, object)"/>.
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
            int current = 0;
            while( current < _processList.Count )
            {
                var type = _processList[current++];
                var g = type._codeGenerator;
                if( g == null )
                {
                    if( type.Type.IsEnum )
                    {
                        monitor.Info( $"Enum '{type.Type:C}' has no TypeScript implementor function. Using the default enum generator." );
                        type.EnsureTypePart( closer: "" )
                            .AppendEnumDefinition( monitor, type.Type, type.TypeName, true );
                    }
                    else
                    {
                        monitor.Warn( $"The type '{type.Type:C}' has no TypeScript implementor function." );
                        required ??= new List<ITSGeneratedType>();
                        required.Add( type );
                    }
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

