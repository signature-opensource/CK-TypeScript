using CK.Core;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System.Collections.Generic;
using System.Linq;

namespace CK.Setup;

/// <summary>
/// Manages the CTSType model for Json serialization.
/// This is available on the <see cref="ITSPocoCodeGenerator.CTSTypeSystem"/> if Json serialization
/// is available.
/// </summary>
public sealed partial class CTSTypeSystem
{
    readonly TypeScriptContext _typeScriptContext;
    readonly IPocoTypeNameMap _jsonExhangeableNames;
    readonly TypeScriptFile _ctsTypeFile;
    readonly ITSDeclaredFileType _symCTS;
    readonly ITSCodePart _initPart;
    readonly ITSFileType _ctsType;
    readonly JsonRequiresHandlingMap _requiresHandlingMap;

    internal CTSTypeSystem( TypeScriptContext context, IPocoTypeNameMap jsonExhangeableNames )
    {
        _typeScriptContext = context;
        _jsonExhangeableNames = jsonExhangeableNames;
        _requiresHandlingMap = new JsonRequiresHandlingMap( jsonExhangeableNames.TypeSet );
        _ctsTypeFile = _typeScriptContext.Root.Root.FindOrCreateTypeScriptFile( "CK/Core/CTSType.ts" );

        _symCTS = _ctsTypeFile.DeclareType( "SymCTS" );
        _ctsTypeFile.Body.Append( "export const SymCTS = Symbol.for(\"CK.CTSType\");" ).NewLine();
        _ctsType = _ctsTypeFile.CreateType( "CTSType", null, null );
        _initPart = _ctsTypeFile.Body.CreatePart();

        _ctsType.TypePart.Append( $$"""
            /**
             * CTSType is currently &lt;any&gt;. Strongly typing it involves to handle null
             * (detect and raise error) in depth.
             * This is not a validator (the backend is up to date by design) and null handling
             * is a (basic) part of validation.
             */
            export const CTSType : any = {
                get typeFilterName(): string {return "{{context.BinPathConfiguration.TypeFilterName}}"; },
                toTypedJson( o: any ) : [string,unknown]|null {
                    if( o == null ) return null;
                    const t = o[SymCTS];
                    if( !t ) throw new Error( "Untyped object. A type must be specified with CTSType." );
                    return [t.name, t.json( o )];
                },
                fromTypedJson( o: any ) : unknown {
                    if( o == null ) return undefined;
                    if( !(o instanceof Array && o.length === 2) ) throw new Error( "Expected 2-cells array." );
                    const t = (<any>CTSType)[o[0]];
                    if( !t ) throw new Error( `Invalid type name: ${o[0]}.` );
                    if( !t.set ) throw new Error( `Type name '${o[0]}' is not serializable.` );
                    const j = t.nosj( o[1] );
                    return j !== null && typeof j === 'object' ? t.set( j ) : j;
               },
               stringify( o: any, withType: boolean = true ) : string {
                   const t = CTSType.toTypedJson( o );
                   return JSON.stringify( withType ? t : t[1] );
               },
               parse( s: string ) : unknown {
                   return CTSType.fromTypedJson( JSON.parse( s ) );
               },
            
            """ );

        if( context.Root.ReflectTS )
        {
            Throw.DebugAssert( "ReflectTS is true.", context.Root.TSTypes.ReflectTSTypeFile != null );
            _ctsTypeFile.Imports.ImportFromFile( context.Root.TSTypes.ReflectTSTypeFile, "TSType" );
        }

        context.Root.AfterCodeGeneration += OnAfterCodeGeneration;
    }

    void OnAfterCodeGeneration( object? sender, EventMonitoredArgs e )
    {
        foreach( var t in _jsonExhangeableNames.TypeSet.NonNullableTypes )
        {
            if( t.Kind is PocoTypeKind.Basic or PocoTypeKind.Record or PocoTypeKind.PrimaryPoco )
            {
                if( _typeScriptContext.Root.TSTypes.Find( t ) is not ITSFileType ts ) continue;

                Throw.DebugAssert( "No need to map: these types are not mapped.", t == PocoCodeGenerator.MapType( t ) );
                Throw.DebugAssert( "No need to map: these types are not mapped.", t.IsSerializedObservable );

                var ctorBody = ts.TypePart.FindKeyedPart( ITSKeyedCodePart.ConstructorBodyPart );
                if( ctorBody != null )
                {
                    ctorBody.File.Imports.ImportFromFile( _ctsType.File, "CTSType" );
                    ctorBody.Append( "CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( t ) ).Append( "].set( this );" ).NewLine();
                }
                else
                {
                    if( !ts.IsPrimitive )
                    {
                        e.Monitor.Warn( $"ConstructorBodyPart not found type '{t}'. " +
                                        $"This type should have been generated in a file and its constructor should contain a \"ConstructorBody\" part." );
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the CTSType file that defines the static <see cref="CTSType"/> and <see cref="SymCTS"/> types.
    /// </summary>
    public TypeScriptFile CTSTypeFile => _ctsTypeFile;

    /// <summary>
    /// Gets the symbol used to type objects with their associated CTSType.
    /// This is exposed so that <see cref="ITSFileImportSection.Import(ITSType)"/> can easily import it.
    /// </summary>
    public ITSType SymCTS => _symCTS;

    /// <summary>
    /// Gets the static CTSType object.
    /// This is exposed so that <see cref="ITSFileImportSection.Import(ITSType)"/> can easily import it.
    /// </summary>
    public ITSType CTSType => _ctsType;

    /// <summary>
    /// Gets the exchangeable name map.
    /// </summary>
    public IPocoTypeNameMap JsonExhangeableNames => _jsonExhangeableNames;

    /// <summary>
    /// Finds or creates a "ctsName" entry in the CTSType object with:
    /// <list type="bullet">
    ///     <item>a "ctsName" field with its Json exchangeable name.</item>
    ///     <item>a "tsType" field to the associated TSType.</item>
    ///     <item>a "isAbstract" property that is true for object (any), abstract poco and union type.</item>
    ///     <item>if isAbstract is false, a "set( T ) : T" method to tag an object with its CTSType entry.</item>
    /// </list>
    /// </summary>
    internal ITSKeyedCodePart EnsureCTSEntry( IPocoType t, ITSType ts )
    {
        Throw.DebugAssert( !t.IsNullable && !ts.IsNullable && t == PocoCodeGenerator.MapType( t ) );

        var ctsName = _jsonExhangeableNames.GetName( t );
        var part = _ctsType.TypePart.FindOrCreateKeyedPart( ctsName, closer: "},\n" );
        if( part.IsEmpty )
        {
            part.AppendSourceString( ctsName ).Append( ": {" ).NewLine()
                .Append( "name: " ).AppendSourceString( ctsName ).Append( "," ).NewLine();
            if( _typeScriptContext.Root.ReflectTS )
            {
                part.Append( "tsType: TSType[" ).AppendSourceString( ts.TypeName ).Append( "]," ).NewLine();
            }
            if( t.IsSerializedObservable )
            {
                part.File.Imports.Import( ts );
                part.Append( "set( o: " ).Append( ts.TypeName ).Append( " ): " ).Append( ts.TypeName ).Append( " { " );
                if( t.Kind == PocoTypeKind.Enum )
                {
                    part.Append( "o = <" ).Append( ts.TypeName ).Append( ">new Number( o );" );
                }
                else if( ts.IsPrimitive )
                {
                    part.Append( "o = Object( o );" );
                }
                part.Append( " (o as any)[SymCTS] = this; return o; }," ).NewLine();
            }
            if( t is ICollectionPocoType c )
            {
                if( _typeScriptContext.Root.ReflectTS )
                {
                    part.Append( "itemTypes: [ " );
                    WriteItemTypes( part, c.ItemTypes );
                    part.Append( " ]," ).NewLine();
                }
                GenerateCollectionJsonFunction( part, c );
                GenerateCollectionNosjFunction( part, c );
            }
            else if( ts is TSUnionType tU )
            {
                if( _typeScriptContext.Root.ReflectTS )
                {
                    part.Append( "allowedTypes: [ " );
                    WriteItemTypes( part, tU.Types.Select( uT => uT.PocoType ) );
                    part.Append( " ]," ).NewLine();
                }
            }
            else if( t.Kind == PocoTypeKind.Basic )
            {
                // We always provide a json method, even if it is "return o;" (for types that support toJSON()).
                GenerateBasicJsonFunction( part, t, ts );
                Throw.DebugAssert( t.IsSerializedObservable );
                GenerateBasicNosjFunction( t, ts, part );
            }
            else if( t.Kind == PocoTypeKind.Enum )
            {
                Throw.DebugAssert( t.IsSerializedObservable );
                // This one is useless (enums are not in the _requiresHandlingMap
                // but it doesn't cost much (and will not be called).
                part.Append( "json( o: any ) { return o; }," ).NewLine();
                // Normalize null to undefined. This is useful.
                part.Append( "nosj( o: any ) { return o == null ? undefined : o; }," ).NewLine();
            }
        }
        return part;

    }

    void WriteItemTypes( ITSKeyedCodePart part, IEnumerable<IPocoType> types )
    {
        bool atLeastOne = false;
        foreach( var item in types )
        {
            if( atLeastOne ) part.Append( ", " );
            atLeastOne = true;
            part.Append( "{ " ).NewLine();
            WriteIsNullableAndType( part, item, isField: false );
            part.Append( "}" );
        }
    }

    void WriteIsNullableAndType( ITSCodePart part, IPocoType t, bool isField )
    {
        part.Append( "isNullable: " ).Append( t.IsNullable ).Append( "," ).NewLine()
            .Append( "get type(): any { return " )
            .Append( "CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( PocoCodeGenerator.MapType( t ).NonNullable ) )
            .Append( "]; }" );
        if( isField )
        {
            // Here may come the FieldModelPart if needed.
            part.Append( "," ).NewLine();
        }
    }

    internal void EnsureMappingForAnonymousRecord( IRecordPocoType t, ITSType ts, IEnumerable<TSField> fields, bool useTupleSyntax )
    {
        Throw.DebugAssert( "If the type is not using the [tuple] syntax, then its json function must handle it.",
                            useTupleSyntax || _requiresHandlingMap.Contains( t ) );
        var part = EnsureCTSEntry( t, ts );
        GenerateAnonymousRecordJsonFunction( part, t, fields, useTupleSyntax );
        GenerateAnonymousRecordNosjFunction( part, fields, useTupleSyntax );
    }

    internal void OnGeneratingNamedRecord( GeneratingNamedRecordPocoEventArgs e )
    {
        HandleComposite( e.TSGeneratedType, e.RecordPocoType, e.Fields.Select( f => f.TSField ) );
    }

    internal void OnGeneratingPrimaryPoco( GeneratingPrimaryPocoEventArgs e )
    {
        HandleComposite( e.TSGeneratedType, e.PrimaryPocoType, e.Fields.Select( f => f.TSField ) );
    }

    void HandleComposite( ITSFileCSharpType tsType, IPocoType t, IEnumerable<TSField> fields )
    {
        Throw.DebugAssert( !t.IsNullable && t == PocoCodeGenerator.MapType( t ) );
        var ctsName = _jsonExhangeableNames.GetName( t );
        var part = _ctsType.TypePart.FindKeyedPart( ctsName );
        Throw.DebugAssert( part != null );

        GenerateCompositeJsonFunction( part, t, fields );
        GenerateCompositeNosjFunction( part, tsType, t, fields );
    }

}
