using CK.Core;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CK.Setup;

public sealed partial class CTSTypeSystem
{
    void GenerateBasicJsonFunction( ITSKeyedCodePart part, IPocoType t, ITSType ts )
    {
        part.Append( "json( o: any ) {" );
        if( t.Type == typeof( decimal ) )
        {
            // Ok... This is badly designed but introducing a "library initialization snippet" somewhere
            // and deciding where to apply it is not that simple. Moreover this global adaptation is required
            // only for serialization scenario.
            // 
            // At least we have a way to detect that the library used is decimal.js-light or decimal.js...
            if( _initPart.File.Imports.ImportedLibraryNames.Any( n => n == LibraryManager.DecimalJS || n == LibraryManager.DecimalJSLight ) )
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
            // We do what we can here. In a perfect world, toMillis() should return a bigint instead
            // of a number. We simply add '0000' to "convert" into 10th of microseconds (100 nanoseconds).
            part.Append( "return o != null ? o.toMillis().toString()+'0000' : null;" );
        }
        else if( t.Type == typeof( long ) || t.Type == typeof( ulong ) || t.Type == typeof( BigInteger ) )
        {
            // These are mapped to bigint and this primitive type has no toJson support.
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

    void GenerateCollectionJsonFunction( ITSKeyedCodePart part, ICollectionPocoType c )
    {
        Throw.DebugAssert( c == PocoCodeGenerator.MapType( c ) && c.ItemTypes.All( a => a == PocoCodeGenerator.MapType( a ) ) );

        part.Append( "json( o: any ) {" ).NewLine();
        if( c.Kind is PocoTypeKind.Array or PocoTypeKind.List )
        {
            if( !_requiresHandlingMap.Contains( c ) )
            {
                part.Append( "return o;" );
            }
            else if( c.ItemTypes[0].IsPolymorphic )
            {
                part.Append( "return o != null ? o.map( CTSType.toTypedJson ) : null;" );
            }
            else
            {
                part.Append( "if( o == null ) return null;" ).NewLine()
                    .Append( "const t = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( c.ItemTypes[0].NonNullable ) ).Append( "];" ).NewLine()
                    .Append( "return o.map( t.json );" );
            }
        }
        else if( c.Kind is PocoTypeKind.HashSet )
        {
            if( c.ItemTypes[0].IsPolymorphic )
            {
                part.Append( "return o != null ? Array.from( o ).map( CTSType.toTypedJson ) : null;" );
            }
            else if( !_requiresHandlingMap.Contains( c ) )
            {
                part.Append( "return o != null ? Array.from( o ) : null;" );
            }
            else
            {
                part.Append( "if( !o ) return null;" ).NewLine()
                    .Append( "const t = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( c.ItemTypes[0].NonNullable ) ).Append( "];" ).NewLine()
                    .Append( "return Array.from( o ).map( t.json );" );
            }
        }
        else
        {
            Throw.DebugAssert( c.Kind is PocoTypeKind.Dictionary );
            var vType = c.ItemTypes[1];
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
                                r[i[0]] = CTSType.toTypedJson(i[1]);
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
                    string aKey = GetMapAccessor( _jsonExhangeableNames, _requiresHandlingMap, part, c, 0 );
                    string aVal = GetMapAccessor( _jsonExhangeableNames, _requiresHandlingMap, part, c, 1 );

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

        static string GetMapAccessor( IPocoTypeNameMap jsonNames, JsonRequiresHandlingMap requiresMap, ITSKeyedCodePart part, ICollectionPocoType c, int i )
        {
            var vMap = $"i[{i}]";
            var t = c.ItemTypes[i];
            if( t.IsPolymorphic )
            {
                vMap = $"CTSType.toTypedJson({vMap})";
            }
            else if( requiresMap.Contains( t ) )
            {
                part.Append( $"const t{i} = CTSType[" ).AppendSourceString( jsonNames.GetName( c.ItemTypes[i].NonNullable ) ).Append( "];" ).NewLine();
                vMap = $"t{i}.json({vMap})";
            }
            return vMap;
        }

    }

    void GenerateCompositeJsonFunction( ITSKeyedCodePart part, IPocoType t, IEnumerable<TSField> fields )
    {
        part.Append( "json( o: any ) {" );
        if( _requiresHandlingMap.Contains( t ) )
        {
            part.Append( "if( !o ) return null;" ).NewLine()
                .Append( "let r = {} as any;" ).NewLine();
            foreach( var field in fields )
            {
                part.Append( "r." ).Append( field.FieldName ).Append( " = " );
                var tF = PocoCodeGenerator.MapType( field.PocoField.Type ).NonNullable;
                if( tF.IsPolymorphic )
                {
                    part.Append( "CTSType.toTypedJson( o." ).Append( field.FieldName ).Append( " )" );
                }
                else if( _requiresHandlingMap.Contains( tF ) )
                {
                    part.Append( $"CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( tF ) ).Append( "].json( " )
                        .Append( "o." ).Append( field.FieldName ).Append( " )" );
                }
                else
                {
                    part.Append( "o." ).Append( field.FieldName );
                }
                part.Append( ";" ).NewLine();
            }
            part.Append( "return r;" );
        }
        else
        {
            part.Append( "return o;" );
        }
        part.NewLine().Append( "}," ).NewLine();
    }

    void GenerateAnonymousRecordJsonFunction( ITSKeyedCodePart part, IPocoType t, IEnumerable<TSField> fields, bool useTupleSyntax )
    {
        part.Append( "json( o: any ) {" );
        if( _requiresHandlingMap.Contains( t ) )
        {
            part.Append( "if( !o ) return null;" ).NewLine()
                .Append( "return [ " );
            int i = 0;
            foreach( var field in fields )
            {
                if( i != 0 ) part.Append( ", " );
                string accessor = useTupleSyntax ? $"o[{i}]" : $"o.{_typeScriptContext.Root.ToIdentifier( field.PocoField.Name )}";
                var tF = PocoCodeGenerator.MapType( field.PocoField.Type );
                if( tF.IsPolymorphic )
                {
                    part.Append( "CTSType.toTypedJson( " ).Append( accessor ).Append( " )" );
                }
                else if( _requiresHandlingMap.Contains( tF ) )
                {
                    part.Append( $"CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( tF ) ).Append( "].json( " )
                        .Append( accessor ).Append( " )" );
                }
                else
                {
                    part.Append( accessor );
                }
                ++i;
            }
            part.Append( " ];" );
        }
        else
        {
            part.Append( "return o;" );
        }
        part.NewLine().Append( "}," ).NewLine();
    }
}
