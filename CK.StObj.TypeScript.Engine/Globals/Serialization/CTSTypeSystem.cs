using CK.Core;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static CK.Setup.IReadOnlyPocoTypeSet;

namespace CK.Setup
{
    /// <summary>
    /// Manages the CTSType model for Json serialization.
    /// This is available on <see cref="TypeScriptContext.CTSTypeSystem"/> only when Json serialization
    /// in available in the generation context.
    /// </summary>
    public sealed partial class CTSTypeSystem
    {
        readonly TypeScriptContext _typeScriptContext;
        readonly IPocoTypeNameMap _jsonExhangeableNames;
        readonly ITSCodePart _initPart;
        readonly ITSCodePart _ctsType;
        readonly RequiresHandlingMap _requiresHandlingMap;

        internal CTSTypeSystem( TypeScriptContext typeScriptContext, IPocoTypeNameMap jsonExhangeableNames )
        {
            _typeScriptContext = typeScriptContext;
            _jsonExhangeableNames = jsonExhangeableNames;
            _requiresHandlingMap = new RequiresHandlingMap( jsonExhangeableNames.TypeSet );
            var file = _typeScriptContext.Root.Root.FindOrCreateFile( "CK/Core/CTSType.ts" );
            file.Imports.EnsureImport( typeScriptContext.Root.TSTypes.TSTypeFile, "TSType" );

            _initPart = file.Body.CreatePart();
            _ctsType = file.Body.Append( """
                             export const SymCTS = Symbol.for("CK.CTSType");
                             export const CTSType: any  = {
                                 toTypedJson( o: any ) : unknown {
                                     if( o === null || typeof o === "undefined" ) return null;
                                     const t = o[SymCTS];
                                     if( !t ) throw new Error( "Untyped object. A type must be specified with CTSType." );
                                     return [t.name, t.json( o )];
                                 },
                                 fromTypedJson( o: any ) : unknown {
                                     if( o == null ) return null;
                                     if( !(o instanceof Array && o.length === 2) ) throw new Error( "Expected 2-cells array." );
                                     var t = CTSType[o[0]];
                                     if( !t ) throw new Error( `Invalid type name: {{o[0]}}.` );
                                     if( !t.nosj ) throw new Error( `Type name '{{o[0]}}' is abstract.` );
                                     return t.nosj( o[1] );
                                },
                                stringify( o: any ) : string {
                                    return JSON.stringify( CTSType.toTypedJson( o ) );
                                },
                                parse( s: string ) : unknown {
                                    return CTSType.fromTypedJson( JSON.parse( s ) );
                                },
                             """ )
                            .CreatePart( closer: "}\n" );

            typeScriptContext.Root.AfterCodeGeneration += OnAfterCodeGeneration;
        }

        void OnAfterCodeGeneration( object? sender, TypeScriptRoot.AfterCodeGenerationEventArgs e )
        {
            // Skip if some types failed to be resolved.
            if( e.RequiredTypes.Any() ) return;

            foreach( var t in _jsonExhangeableNames.TypeSet.NonNullableTypes )
            {
                if( t.Kind is PocoTypeKind.Basic or PocoTypeKind.Record or PocoTypeKind.PrimaryPoco )
                {
                    var ts = _typeScriptContext.Root.TSTypes.Find( t ) as ITSFileType;
                    if( ts == null ) continue;

                    var ctorBody = ts.TypePart.FindKeyedPart( ITSKeyedCodePart.ConstructorBodyPart );
                    if( ctorBody != null )
                    {
                        ctorBody.File.Imports.EnsureImport( _ctsType.File, "CTSType" );
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
        /// Gets the CTSType object that contains the CSharp names mapping.
        /// </summary>
        public ITSCodePart CTSType => _ctsType;

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
        internal ITSKeyedCodePart EnsureMapping( IPocoType t, ITSType ts )
        {
            Throw.DebugAssert( !t.IsNullable && !ts.IsNullable && t.IsRegular );
            var ctsName = _jsonExhangeableNames.GetName( t );
            var part = _ctsType.FindOrCreateKeyedPart( ctsName, closer: "},\n" );
            if( part.IsEmpty )
            {
                part.AppendSourceString( ctsName ).Append( ": {" ).NewLine()
                    .Append( "name: " ).AppendSourceString( ctsName ).Append( "," ).NewLine()
                    .Append( "tsType: TSType[" ).AppendSourceString( ts.TypeName ).Append( "]," ).NewLine();
                var canSetType = t.Kind != PocoTypeKind.Any
                                 && t.Kind != PocoTypeKind.AbstractPoco
                                 && t.Kind != PocoTypeKind.UnionType;
                if( canSetType )
                {
                    part.File.Imports.EnsureImport( ts );
                    part.Append( "set( o: " ).Append( ts.TypeName ).Append( " ): " ).Append( ts.TypeName ).Append( " { " );
                    if( t.Kind == PocoTypeKind.Enum )
                    {
                        part.Append( "o = new Number( o );" );
                    }
                    else if( ts.IsPrimitive )
                    {
                        part.Append( "o = new " ).Append( ts.TypeName ).Append("( o );");
                    }
                    part.Append( " (o as any)[SymCTS] = this; return o; }," ).NewLine();
                }
                if( t is ICollectionPocoType c )
                {
                    part.Append( "itemTypes: [ " );
                    WriteItemTypes( part, c.ItemTypes );
                    part.Append( " ]," ).NewLine();

                    GenerateCollectionJsonFunction( part, c );
                }
                else if( ts is TSUnionType tU )
                {
                    part.Append( "allowedTypes: [ " );
                    WriteItemTypes( part, tU.Types.Select( uT => uT.PocoType ) );
                    part.Append( " ]," ).NewLine();
                }
                else if( t.Kind == PocoTypeKind.Basic )
                {
                    // We always provide a json method, even if it is "return o;" (for types that support toJSON()).
                    GenerateBasicJsonFunction( t, ts, part );
                }
                else if( t.Kind == PocoTypeKind.Enum )
                {
                    part.Append( "json( o: any ) { return o; }" ).NewLine();
                }
            }
            return part;

        }

        void GenerateCollectionJsonFunction( ITSKeyedCodePart part, ICollectionPocoType c )
        {
            part.Append( "json( o: any ) {" ).NewLine();
            if( c.Kind is PocoTypeKind.Array or PocoTypeKind.List )
            {
                if( !_requiresHandlingMap.Contains( c ) )
                {
                    part.Append( "return o;" );
                }
                else
                {
                    if( c.ItemTypes[0].IsPolymorphic )
                    {
                        part.Append( "return !!o ? o.map( CTSType.typedJson ) : null;" );
                    }
                    else
                    {
                        part.Append( "if( !o ) return null;" ).NewLine()
                            .Append( "const t = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( c.ItemTypes[0].NonNullable ) ).Append( "];" ).NewLine()
                            .Append( "return o.map( t.json );" );
                    }
                }
            }
            else if( c.Kind is PocoTypeKind.HashSet )
            {
                if( !_requiresHandlingMap.Contains( c ) )
                {
                    part.Append( "return !!o ? Array.from( o ) : null;" );
                }
                else
                {
                    if( c.ItemTypes[0].IsPolymorphic )
                    {
                        part.Append( "return !!o ? Array.from( o ).map( CTSType.typedJson ) : null;" );
                    }
                    else
                    {
                        part.Append( "if( !o ) return null;" ).NewLine()
                            .Append( "const t = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( c.ItemTypes[0].NonNullable ) ).Append( "];" ).NewLine()
                            .Append( "return Array.from( o ).map( t.json );" );
                    }
                }
            }
            else
            {
                Throw.DebugAssert( c.Kind is PocoTypeKind.Dictionary );
                var vType = c.ItemTypes[1];
                Throw.DebugAssert( "Because we are on a regular collection.", vType.IsRegular );
                if( c.ItemTypes[0].Type == typeof( string ) )
                {
                    if( !_requiresHandlingMap.Contains( vType ) )
                    {
                        part.Append( "return o != null ? Object.fromEntries(o.entries()) : null;" );
                    }
                    else if( vType.IsPolymorphic )
                    {
                        part.Append( """
                                if( !o ) return null;
                                let r = {} as any;
                                for( const i of o ) {
                                    r[i[0]] = CTSType.typedJson(i[1]);
                                }
                                return r;
                                """ );
                    }
                    else
                    {
                        part.Append( "if( !o ) return null;" ).NewLine()
                            .Append( "const t = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( vType.NonNullable ) ).Append( "];" ).NewLine()
                            .Append( """
                                    let r = {} as any;
                                    for( const i of o ) {
                                        r[i[0]] = t.json(i[1]);
                                    }
                                    return r;
                                    """ );
                    }
                }
                else
                {
                    if( !_requiresHandlingMap.Contains( c ) )
                    {
                        part.Append( "return o != null ? Array.from( o ) : null;" );
                    }
                    else
                    {
                        Throw.DebugAssert( "Dictionary key invariants.",
                                            c.ItemTypes[0].IsReadOnlyCompliant && !c.ItemTypes[0].IsNullable && !c.ItemTypes[0].IsPolymorphic );
                        string aKey = GetMapAccessor( part, c, 0 );
                        string aVal = GetMapAccessor( part, c, 1 );

                        part.Append( $$"""
                                if( !o ) return null;
                                const r = new Array<[any,any]>;
                                for( const i of o ) {
                                    r.push([{{aKey}},{{aVal}}]);
                                }
                                return r;
                                """ );
                    }
                }
            }
            part.NewLine().Append( "}," ).NewLine();
        }

        void GenerateBasicJsonFunction( IPocoType t, ITSType ts, ITSKeyedCodePart part )
        {
            part.Append( "json( o: any ) {" );
            if( t.Type == typeof( decimal ) )
            {
                // Ok... This is badly designed but introducing a "library initialization snippet" somewhere
                // and deciding where to apply it is not that simple. Moreover this global adaptation is required
                // only for serialization scenario.
                // 
                // At least we have a way to detect that the library used is decimal.js-light or decimal.js...
                if( _initPart.File.Imports.ImportedLibraryNames.Any( n => n == TypeScriptRoot.DecimalJS || n == TypeScriptRoot.DecimalJSLight ) )
                {
                    _initPart.Append(
                        """
                                // This configures the default Decimal to be compliant with .Net Decimal
                                // type. Precision is boosted from 20 to 29 and toExpNeg/Pos are set so 
                                // that values out of range with .Net type will be toString() and toJSON() 
                                // with an exponential notation (that will fail the .Net parsing).
                                // However nothing prevents a javascript Decimal to be out of range but
                                // in this case, parsing on the .Net side will fail. For instance:
                                // Decimal.MaxValue + 1 = 79228162514264337593543950336 will fail regardless
                                // of the notation used.
                                Decimal.set({
                                      precision: Math.max( 29, Decimal.precision ), 
                                      toExpNeg: Math.min( -29, Decimal.toExpNeg ),
                                      toExpPos: Math.max( 29, Decimal.toExpPos )
                                }); 

                                """ );
                }
                Throw.DebugAssert( "The Decimal whatever it is must support toJSON().", _requiresHandlingMap.HasToJSONMethod( t ) );
                part.Append( "return o;" );
            }
            else if( t.Type == typeof( TimeSpan ) )
            {
                // We do what we can here. In a perfect world, toMillis() should return a BigInt instead
                // of a Number. We simply add '0000' to "convert" into 10th of microseconds (100 nanoseconds).
                part.Append( "return o != null ? o.toMillis().toString()+'0000' : null;" );
            }
            else if( t.Type == typeof( long ) || t.Type == typeof( ulong ) || t.Type == typeof( BigInteger ) || t.Type == typeof( long ) )
            {
                // These are mapped to BigInt and this primitive type has no toJson support.
                // See https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt#use_within_json.
                // The toString() method is fine for us.
                part.Append( "return o != null ? o.toString() : null;" );
            }
            else
            {
                // MCString and CodeString are excluded from TypeScript exchangeable set.
                // We are left with the primitive types or the types that we know to have a toJSON() method.
                if( !ts.IsPrimitive && !_requiresHandlingMap.HasToJSONMethod( t ) )
                {
                    Throw.CKException( $"Unsuported Basic type '{t.Type}'." );
                }
                part.Append( "return o;" );
            }
            part.Append( " }," ).NewLine();
        }

        string GetMapAccessor( ITSKeyedCodePart part, ICollectionPocoType c, int i )
        {
            var vMap = $"i[{i}]";
            var t = c.ItemTypes[i];
            if( t.IsPolymorphic )
            {
                vMap = $"CTSType.typedJson({vMap})";
            }
            else if( _requiresHandlingMap.Contains( t ) )
            {
                part.Append( $"const t{i} = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( c.ItemTypes[i].NonNullable ) ).Append( "];" ).NewLine();
                vMap = $"t{i}.json({vMap})";
            }
            return vMap;
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
                .Append( "CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( t.NonNullable ) )
                .Append( "]; }" );
            if( isField )
            {
                // Here may come the FieldModelPart if needed.
                part.Append( "," ).NewLine();
            }
        }

        internal void EnsureMappingForAnonymousRecord( IRecordPocoType t, ITSType ts, IEnumerable<TSField> fields )
        {
            var part = EnsureMapping( t, ts );
            HandleComposite( part, t, fields );
        }

        internal void OnGeneratingNamedRecord( GeneratingNamedRecordPocoEventArgs e )
        {
            HandleComposite( e.RecordPocoType, e.Fields.Select( f => f.TSField ) );
        }

        internal void OnGeneratingPrimaryPoco( GeneratingPrimaryPocoEventArgs e )
        {
            HandleComposite( e.PrimaryPocoType, e.Fields.Select( f => f.TSField ) );
        }

        void HandleComposite( IPocoType t, IEnumerable<TSField> fields )
        {
            Throw.DebugAssert( !t.IsNullable );
            var ctsName = _jsonExhangeableNames.GetName( t );
            var part = _ctsType.FindKeyedPart( ctsName );
            Throw.DebugAssert( part != null );

            HandleComposite( part, t, fields );
        }

        void HandleComposite( ITSKeyedCodePart part, IPocoType t, IEnumerable<TSField> fields )
        {
            part.Append( "json( o: any ) {" );
            if( _requiresHandlingMap.Contains( t ) )
            {
                part.Append( "if( !o ) return null;" ).NewLine()
                    .Append( "let r = {} as any;" ).NewLine();
                foreach( var field in fields )
                {
                    part.Append( "r." ).AppendIdentifier( field.PocoField.Name ).Append( " = " );
                    if( field.PocoField.Type.IsPolymorphic )
                    {
                        part.Append( "CTSType.typedJson( o." ).AppendIdentifier( field.PocoField.Name ).Append( " )" );
                    }
                    else if( _requiresHandlingMap.Contains( field.PocoField.Type ) )
                    {
                        Throw.DebugAssert( "Field types belongs to the exchageable PocoTypeSet, abstract read only collections are filtered out.",
                                           field.PocoField.Type.RegularType != null );
                        part.Append( $"CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( field.PocoField.Type.RegularType.NonNullable ) ).Append( "].json( " )
                            .Append( "o." ).AppendIdentifier( field.PocoField.Name ).Append( " )" );
                    }
                    else
                    {
                        part.Append( "o." ).AppendIdentifier( field.PocoField.Name );
                    }
                    part.Append( ";" ).NewLine();
                }
                part.Append( "return r;" ).NewLine();
            }
            else
            {
                part.Append( "return o;" );
            }
            part.NewLine().Append( "}," ).NewLine();
        }

    }

}
