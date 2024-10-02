using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Handles a map from "type key" to <see cref="ITSType"/>. The type key can be a C# type but may be another object.
/// <para>
/// A <see cref="Type"/> key is enough for value types since the non nullable type and the nullable type can be registered, this is
/// what <see cref="RegisterStandardTypes(IActivityMonitor, bool, bool, bool, bool)"/> does. To be able to handle reference type nullability,
/// the key can be any object.
/// </para>
/// <para>
/// The <see cref="object"/> type is mapped to "{}" (this is the TS type that contains object and javascript primitive types except undefined and nul),
/// with no default values, no imports and no capacity to write any values by itself. To register other basic types, <see cref="RegisterStandardTypes(IActivityMonitor, bool, bool, bool, bool)"/>
/// must be called.
/// </para>
/// </summary>
public sealed partial class TSTypeManager
{
    // Only non nullable types. Index is available in ITSType.Index.
    readonly List<TSType> _allTypes;
    // Null value is used to detect reentrancy while resolving.
    readonly Dictionary<object, ITSType?> _types;
    readonly Dictionary<string, LibraryImport> _libraries;
    readonly TypeScriptRoot _root;
    // Not null when TypeScriptRoot.ReflectTS is true.
    readonly ITSCodePart? _reflectTSTypes;
    // New TSGeneratedType are appended to this list: GenerateCode loops until no new type appears in this list.
    readonly List<TSDeferredType> _processList;
    bool _generateCodeDone;

    internal TSTypeManager( TypeScriptRoot root )
    {
        _allTypes = new List<TSType>();
        _root = root;
        _libraries = new Dictionary<string, LibraryImport>();
        _types = new Dictionary<object, ITSType?>();
        _processList = new List<TSDeferredType>();
        if( root.ReflectTS )
        {
            _reflectTSTypes = root.Root.FindOrCreateTypeScriptFile( "CK/Core/TSType.ts" )
                            .Body.Append( "export const TSType = {" )
                                 .CreatePart( closer: "}\n" );
        }
        _types.Add( typeof( object ), new TSBasicType( this, "{}", null, null ) );
    }

    /// <summary>
    /// Gets the "CK/Core/TSType.ts" file that contains the TSType map that contains
    /// all the <see cref="ITSType.TSTypeModel"/>.
    /// <para>
    /// <see cref="TypeScriptRoot.ReflectTS"/> must be true for for this to be not null.
    /// </para>
    /// <para>
    /// This acts as a meta model that can be used to brand types. It can be extended
    /// to offer more functionalities.
    /// </para>
    /// </summary>
    public TypeScriptFile? ReflectTSTypeFile => _reflectTSTypes?.File;

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
    /// Use the indexer <see cref="this[object]"/> to throw if the type must already be mapped.
    /// Use the <see cref="FindByTypeName(string)"/> to find a TSType by its <see cref="ITSType.TypeName"/>.
    /// </summary>
    /// <param name="keyType">The key type for which a TS type should be found.</param>
    /// <returns>The TS type or null if not found.</returns>
    public ITSType? Find( object keyType ) => _types.GetValueOrDefault( keyType );

    /// <summary>
    /// Gets a registered TS type for a type or throws an <see cref="KeyNotFoundException"/>.
    /// </summary>
    /// <param name="keyType">The key type for which a TS type should be found.</param>
    /// <returns>The TS type.</returns>
    public ITSType this[object keyType] => _types[keyType] ?? throw new KeyNotFoundException( $"Key type '{keyType}' is currently resolving." );

    /// <summary>
    /// Gets whether <see cref="TypeScriptRoot.GenerateCode(IActivityMonitor)"/> has been called: no more
    /// types can be registered.
    /// </summary>
    public bool GenerateCodeDone => _generateCodeDone;

    internal int Register( TSType tsType, out ITSCodePart? model )
    {
        Throw.CheckState( GenerateCodeDone is false );
        _types.Add( tsType.TypeName, tsType );
        _allTypes.Add( tsType );
        if( _reflectTSTypes != null )
        {
            int index = _allTypes.Count;
            if( !_reflectTSTypes.IsEmpty ) _reflectTSTypes.Append( "," ).NewLine();
            model = _reflectTSTypes.AppendSourceString( tsType.TypeName ).Append( ": {" ).NewLine()
                            .Append( "tsName: " ).AppendSourceString( tsType.TypeName ).Append( "," ).NewLine()
                            .Append( "index: " ).Append( index.ToString( CultureInfo.InvariantCulture ) ).Append( "," )
                            .CreateKeyedPart( tsType, closer: "}" );
        }
        else
        {
            model = null;
        }
        return _allTypes.Count;
    }

    /// <summary>
    /// Tries to find a TypeScript type from its <see cref="ITSType.TypeName"/>.
    /// </summary>
    /// <param name="typeName">The type name to lookup.</param>
    /// <returns>The type or null.</returns>
    public ITSType? FindByTypeName( string typeName ) => _types.GetValueOrDefault( typeName );

    /// <summary>
    /// Registers a new mapping from C# type to <see cref="ITSType"/> mapping.
    /// This throws a <see cref="ArgumentException"/> if the key is already mapped.
    /// <para>
    /// It is often easier to use <see cref="TypeScriptFolder.FindOrCreateTSManualFile(NormalizedPath)"/> and then
    /// <see cref="TSManualFile.CreateType(string, Action{ITSFileImportSection}?, string?, string)"/> to manually
    /// manage types.
    /// </para>
    /// </summary>
    /// <param name="type">The C# reference type.</param>
    /// <param name="tsType">The associated TS type.</param>
    public void RegisterType( Type type, ITSType tsType )
    {
        Throw.CheckNotNullArgument( tsType );
        Throw.CheckArgument( !tsType.IsNullable );
        Throw.CheckState( GenerateCodeDone is false );
        if( type.IsValueType )
        {
            if( Nullable.GetUnderlyingType( type ) != null )
            {
                Throw.ArgumentException( "Nullable value type cannot be registered." );
            }
            _types.Add( typeof(Nullable<>).MakeGenericType( type ), tsType.Nullable );
        }
        _types.Add( type, tsType );
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
        Throw.CheckState( GenerateCodeDone is false );
        _types.Add( typeof( T ), tsType );
        _types.Add( typeof( T? ), tsType.Nullable );
    }

    /// <summary>
    /// Raised when a <see cref="ITSFileCSharpType"/> must be configured from a C# type
    /// or a <see cref="RequireTSFromTypeEventArgs.ResolvedType"/> obtained.
    /// </summary>
    public event EventHandler<RequireTSFromTypeEventArgs>? TSFromTypeRequired;

    /// <summary>
    /// Raised when a <see cref="ITSType"/> must be resolved for an object key type
    /// that is not a C# type.
    /// <para>
    /// The TSType must be resolved by setting <see cref="RequireTSFromObjectEventArgs.ResolvedType"/>.
    /// Often, the event handler first calls back <see cref="ResolveTSType(IActivityMonitor, object)"/> with a
    /// C# type to resolve the object to its "underlying type".
    /// </para>
    /// </summary>
    public event EventHandler<RequireTSFromObjectEventArgs>? TSFromObjectRequired;

    /// <summary>
    /// Resolves the mapping from a key type to a <see cref="ITSType"/> by raising <see cref="TSFromTypeRequired"/>
    /// or <see cref="TSFromObjectRequired"/> events.
    /// <para>
    /// When <paramref name="keyType"/> is a C# type, and no mapping exits and <see cref="IsValidGeneratedType(Type)"/> returns false,
    /// this throws an <see cref="ArgumentException"/>.
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
            if( GenerateCodeDone )
            {
                Throw.InvalidOperationException( $"Resolving '{keyType}': No new types muste be registered after Code Generation step." );
            }
            if( keyType is Type t )
            {
                HashSet<Type>? cycleSameFolderDetector = null;
                mapped = ResolveTSTypeFromType( monitor, t, false, ref cycleSameFolderDetector );
                if( mapped == null )
                {
                    // We cannot order the items here but we don't really care.
                    var cycle = _types.Where( kv => kv.Value == null ).Select( kv => kv.Key is Type t ? t.Name : kv.Key.ToString() ).Concatenate( "', '" );
                    Throw.CKException( $"Reentrant TSType resolution detected: '{cycle}'." );
                }
            }
            else
            {
                mapped = ResolveTSTypeFromObject( monitor, keyType );
            }
        }
        Throw.DebugAssert( mapped != null );
        return mapped;
    }

    ITSType ResolveTSTypeFromObject( IActivityMonitor monitor, object keyType )
    {
        var e = new RequireTSFromObjectEventArgs( monitor, keyType );
        TSFromObjectRequired?.Invoke( this, e );
        if( e.ResolvedType == null )
        {
            Throw.CKException( $"Unable to resolve TSType for object '{keyType}'." );
        }
        // This micro model exhibits a reentrancy issue when using the Object key:
        //
        //         public record struct RecTry( string Name, List<RecTry> Others );
        //         public interface IRecTryPoco : IPoco { IList<RecTry> R1 { get; } }
        //
        // Let's firts register an Object, the Poco: {[PrimaryPoco]CK.StObj.TypeScript.Tests.RecordTests.IRecTryPoco}.
        // PrimaryPoco, NamedRecord and Basic types are rerouted to their C# Type. This registers:
        // 	    {Name = "IRecTryPoco" FullName = "CK.StObj.TypeScript.Tests.RecordTests+IRecTryPoco"}
        // Fields resolution is deferred (no reentrancy).
        // The Type is in the cache.
        // There is no issue with the DefaultValue: it is "new PocoTypeName()" and it is ALWAYS available.
        // The Object is in the cache.
        // 
        // Let's now register the RecTry Object: {[Record]CK.StObj.TypeScript.Tests.RecordTests.RecTry}
        // 	Same as the PrimaryPoco case, it is rerouted to its C# resolution:
        // 	    {Name = "RecTry" FullName = "CK.StObj.TypeScript.Tests.RecordTests+RecTry"}
        // 	Type is in the Cache but here the DefaultValue kicks in... Even if the default value is simply $"new TypeName()", 
        // 	to know IF there is a default (i.e. all fields have a defaults), one need to resolve the field types
        // 	and this can lead to an infinite recursion...
        // 	This resolution is slightly deferred by using the builder.DefaultValueSourceProvider: the Type will be registered in the cache
        //  as soon as the call the TSFromTypeRequired event ends and then, before returning the TSType to the caller, its Default value
        //  will be set by calling the DefaultValueSourceProvider that will resolve the fields types:
        // 	-> Resolving the DefaultValueSource for named record RecTry.
        // 		{[Basic]string}
        // 		{[List]List<CK.StObj.TypeScript.Tests.RecordTests.RecTry>}
        // 		-> A list is an inline type: its argument needs to be resolved to compute its TypeName.
        // 		-> We reenter the Object RecTry {[Record]CK.StObj.TypeScript.Tests.RecordTests.RecTry}
        // 		   As usual the Object is rerouted to its Type -> {Name = "RecTry" FullName = "CK.StObj.TypeScript.Tests.RecordTests+RecTry"}
        // 		   but here it is found in the cache: this why the DefaultValue is "slightly deferred".
        // 		   The Object is added to the cache. We are done with it... But in a nested call.
        // When the top level resolution ends, the Object is already in the cache.
        // We have no other choice than to upsert it instead of adding it.
        _types[keyType] = e.ResolvedType;
        return e.ResolvedType;
    }

    ITSType? ResolveTSTypeFromType( IActivityMonitor monitor, Type t, bool internalCall, ref HashSet<Type>? sameFolderDetector )
    {
        if( !IsValidGeneratedType( t ) )
        {
            if( internalCall ) return null;
            Throw.ArgumentException( nameof( t ), $"Invalid type for a TSGeneratedType: '{t.ToCSharpName()}'. It must be deconstructed: array, collections, value tuples and nullable value types must be handled by the caller since these are \"inlined\" types in TypeScript." );
        }
        // Don't set the TypeName, Folder and other configurable properties upfront:
        // by setting them to null a builder configurator can decide to use the default values.
        var e = new RequireTSFromTypeEventArgs( monitor, t, GetSafeName( t ) );
        TSFromTypeRequired?.Invoke( this, e );
        if( e.ResolvedType != null )
        {
            return e.ResolvedType;
        }
        if( string.IsNullOrWhiteSpace( e.TypeName ) )
        {
            e.TypeName = e.DefaultTypeName;
        }
        TypeScriptFolder? folder = null;
        TypeScriptFile? file = null;
        Type? refTarget = e.SameFileAs ?? e.SameFolderAs;
        if( refTarget != null )
        {
            if( !_types.TryGetValue( refTarget, out var target ) )
            {
                sameFolderDetector ??= new HashSet<Type>();
                if( !sameFolderDetector.Add( t ) )
                {
                    Throw.InvalidOperationException( $"TypeScript.SameFoldeAs cycle detected: {sameFolderDetector.Select( c => c.Name ).Concatenate( " => " )}." );
                }
                target = ResolveTSTypeFromType( monitor, refTarget, true, ref sameFolderDetector );
            }
            if( target is not ITSFileCSharpType gTarget )
            {
                monitor.Warn( $"Type '{refTarget:C}' cannot be used in SameFileAs or SameFolderAs attributes since it is not a type associated to a generated file. Type '{t:N}' will be in a folder/file based on its namespace/name." );
            }
            else
            {
                folder = gTarget.File.Folder;
                if( e.SameFileAs != null )
                {
                    file = gTarget.File;
                }
            }
        }
        if( file == null )
        {
            folder ??= _root.Root.FindOrCreateFolder( e.Folder ?? t.Namespace!.Replace( '.', '/' ) );
            file = folder.FindOrCreateTypeScriptFile( e.FileName ?? e.TypeName.Replace( '<', '{' ).Replace( '>', '}' ) + ".ts" );
        }
        if( e.HasError )
        {
            monitor.Error( $"Type '{t:C}' is on error." );
        }
        else
        {
            Throw.DebugAssert( folder == file.Folder );
            var defName = e.TypeName != e.DefaultTypeName
                            ? $" (using explicit type name '{e.TypeName}' instead of default '{e.DefaultTypeName}')"
                            : null;
            monitor.Trace( $"Type '{t:C}' will be generated in '{folder.Path}/{file.Name}'{defName}." );
        }
        if( t.IsEnum )
        {
            // Default enum implementation. Should be okay for 99.99% of the enums.
            e.DefaultValueSource ??= SelectEnumTypeDefaultValue( monitor, e );
            e.TryWriteValueImplementation ??= WriteEnumValue;
            e.Implementor ??= ImplementEnum;
        }
        else if( e.Implementor == null )
        {
            // There is no implementor function. We can provide defaults for some well-known types.
            // 
            // We trigger this default only if there is no implementor function as the default DefaultValueSource and
            // TryWriteValueImplementation method may not be compatible with all TypeScript implementations of the type.

            // Currently, only the Guid is handled.
            if( t == typeof( Guid ) )
            {
                // Default Guid implementation:
                e.DefaultValueSource ??= "Guid.empty";
                e.TryWriteValueImplementation ??= static ( w, t, o ) =>
                {
                    if( o is Guid g )
                    {
                        w.Append( "new Guid(" ).AppendSourceString( g.ToString() ).Append( ")" );
                        return true;
                    }
                    return false;
                };
                e.Implementor = ImplementDefaultGuid;
            }
        }
        var newOne = new TSDeferredType( this,
                                          t,
                                          e.TypeName,
                                          file,
                                          e.DefaultValueSource,
                                          e.TryWriteValueImplementation,
                                          e.Implementor,
                                          e.PartCloser,
                                          e.HasError );
        _processList.Add( newOne );
        _types.Add( t, newOne );
        // Now that the type has been registered, we can resolve the DefaultValueSource.
        if( newOne.DefaultValueSource == null ) newOne.SetDefaultValueSource( e.DefaultValueSourceProvider?.Invoke( monitor, newOne ) );
        return newOne;

        static string GetSafeName( Type t )
        {
            // ExternalName (and more generally type/attribute caching) must definitely be refactored.
            var n = (string?)t.GetCustomAttributesData().Where( d => d.AttributeType.Name == "ExternalNameAttribute"
                                                                     && d.AttributeType.Namespace == "CK.Core" )
                                                        .FirstOrDefault()?
                                                        .ConstructorArguments[0]
                                                        .Value ?? t.Name;
            if( !t.IsGenericType )
            {
                return n;
            }
            Type tDef = t.IsGenericTypeDefinition ? t : t.GetGenericTypeDefinition();
            int idxBackTick = n.IndexOf( '`' );
            return idxBackTick > 0 ? n.Substring( 0, idxBackTick ) : n;
        }
    }

    static bool ImplementDefaultGuid( IActivityMonitor monitor, ITSFileCSharpType type )
    {
        type.TypePart.Append( """
                /**
                * Simple immutable encapsulation of a string. No check is currently done on the 
                * value format that must be in the '00000000-0000-0000-0000-000000000000' form.
                */
                export class Guid {

                    static #empty : Guid;   

                    /**
                    * The empty Guid '00000000-0000-0000-0000-000000000000' is the default.
                    */
                    public static get empty() { return Guid.#empty ??= new Guid('00000000-0000-0000-0000-000000000000'); }
                    
                    constructor( public readonly guid: string ) {

                """ )
            .InsertKeyedPart( ITSKeyedCodePart.ConstructorBodyPart )
            .Append( """
                    }

                    get value() {
                        return this.guid;
                      }

                    toString() {
                        return this.guid;
                      }

                    toJSON() {
                        return this.guid;
                      }
                """ );
        return true;
    }

    static bool ImplementEnum( IActivityMonitor monitor, ITSFileCSharpType type )
    {
        type.TypePart.AppendEnumDefinition( monitor, type.Type, type.TypeName, export: true, leaveTypeOpen: true );
        return true;
    }

    static bool WriteEnumValue( ITSCodeWriter writer, ITSFileCSharpType type, object val )
    {
        if( val.GetType() == type.Type )
        {
            writer.Append( type.TypeName ).Append( "." ).Append( val.ToString() );
            return true;
        }
        return false;
    }

    static string? SelectEnumTypeDefaultValue( IActivityMonitor monitor, RequireTSFromTypeEventArgs d )
    {
        Debug.Assert( d.Type.IsEnum && d.DefaultValueSource == null );
        // [Doc] The elements of the array are sorted by the binary values (that is, the unsigned values)
        //       of the enumeration constants.
        // 
        // => This is perfect for us: if 0 is defined (that is the "normal" default), then it will be
        //    the first value even if negative exist.
        //
        // Note: This reproduces what is done in the PocoTypeSystem. This default TypeScript enum implementation
        //       is compatible with the IEnumPocoType.
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
    /// and cannot have an associated <see cref="ITSFileCSharpType"/>.
    /// </summary>
    /// <param name="t">The type that may require a dedicated file.</param>
    /// <returns>True if the type can be defined by a <see cref="ITSFileCSharpType"/>, false otherwise.</returns>
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

    // Use the monitor for success tracking. This returns the number of TSGeneratedType to generate.
    internal int GenerateCode( IActivityMonitor monitor )
    {
        Throw.DebugAssert( !_generateCodeDone );
        for( int i = 0; i < _processList.Count; i++ )
        {
            TSDeferredType? type = _processList[i];
            if( type.HasError )
            {
                monitor.Error( $"Skipping TS code generation for '{type.TypeName}' that is on error." );
            }
            else
            {
                var g = type._codeGenerator;
                if( g == null )
                {
                    monitor.Error( $"The type '{type.Type:C}' has no TypeScript implementor function." );
                }
                else if( !g( monitor, type ) )
                {
                    monitor.Error( $"TypeScript implementor for type '{type.Type:C}' failed." );
                }
                else if( type.TypePart == null )
                {
                    monitor.Error( $"TypeScript implementor for type '{type.Type:C}' didn't create the TypePart." );
                }
            }
        }
        return _processList.Count;
    }

    internal void SetGeneratedCodeDone( IActivityMonitor monitor )
    {
        monitor.Trace( "Closing type registration. No more types can be registered." );
        _generateCodeDone = true;
    }
}

