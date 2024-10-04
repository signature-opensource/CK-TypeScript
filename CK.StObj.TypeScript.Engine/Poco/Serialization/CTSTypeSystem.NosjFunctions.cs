using CK.Core;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;

namespace CK.Setup;

public sealed partial class CTSTypeSystem
{
    static void GenerateBasicNosjFunction( IPocoType t, ITSType ts, ITSKeyedCodePart part )
    {
        part.Append( "nosj( o: any ) {" );
        if( t.Type == typeof( decimal ) )
        {
            part.Append( "return o != null ? new Decimal( o ) : undefined;" );
        }
        else if( t.Type == typeof( DateTime ) )
        {
            part.Append( "return o != null ? DateTime.fromISO( o, {zone: 'UTC'}) : undefined;" );
        }
        else if( t.Type == typeof( DateTimeOffset ) )
        {
            part.Append( "return o != null ? DateTime.fromISO( o, {setZone: true} ) : undefined;" );
        }
        else if( t.Type == typeof( TimeSpan ) )
        {
            part.Append( "return o != null ? Duration.fromMillis( Number.parseInt(o.substring(0,o.length-4)) ) : undefined;" );
        }
        else if( t.Type == typeof( long ) || t.Type == typeof( ulong ) || t.Type == typeof( BigInteger ) )
        {
            part.Append( "return o != null ? BigInt( o ) : undefined;" );
        }
        else if( t.Type == typeof( Guid ) )
        {
            part.Append( "return o != null ? new Guid( o ) : undefined;" );
        }
        else if( t.Type == typeof( Guid ) || t.Type == typeof( ExtendedCultureInfo ) || t.Type == typeof( NormalizedCultureInfo ) )
        {
            part.Append( $"return o != null ? new {ts.TypeName}( o ) : undefined;" );
        }
        else if( t.Type == typeof( SimpleUserMessage ) )
        {
            part.Append( $"return o != null ? SimpleUserMessage.parse( o ) : undefined;" );
        }
        else
        {
            Throw.DebugAssert( ts.IsPrimitive );
            // Normalize null to undefined.
            part.Append( $"return o == null ? undefined : o;" );
        }
        part.Append( " }," ).NewLine();
    }

    void GenerateCollectionNosjFunction( ITSKeyedCodePart part, ICollectionPocoType c )
    {
        part.Append( "nosj( o: any ) {" ).NewLine()
            .Append( "if( o == null ) return undefined;" ).NewLine();
        if( c.Kind is PocoTypeKind.Array or PocoTypeKind.List )
        {
            part.Append( "if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );" ).NewLine();
            if( c.ItemTypes[0].IsPolymorphic )
            {
                part.Append( "return o.map( CTSType.fromTypedJson );" );
            }
            else
            {
                part.Append( "const t = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( c.ItemTypes[0].NonNullable ) ).Append( "];" ).NewLine()
                    .Append( "return o.map( t.nosj );" );
            }
        }
        else if( c.Kind is PocoTypeKind.HashSet )
        {
            part.Append( "if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );" ).NewLine();
            if( c.ItemTypes[0].IsPolymorphic )
            {
                part.Append( "return new Set( o.map( CTSType.fromTypedJson ) );" );
            }
            else
            {
                part.Append( "const t = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( c.ItemTypes[0].NonNullable ) ).Append( "];" ).NewLine()
                    .Append( "return new Set( o.map( t.nosj ) );" );
            }
        }
        else
        {
            Throw.DebugAssert( c.Kind is PocoTypeKind.Dictionary );
            var vType = c.ItemTypes[1];

            if( c.ItemTypes[0].Type == typeof( string ) )
            {
                part.Append( "const isA = o instanceof Array;" ).NewLine()
                    .Append( "if( !isA && typeof o !== 'object' ) throw new Error( 'Expected Array or Object.' );" ).NewLine()
                    .Append( "if( isA ) {" ).NewLine();
                if( vType.IsPolymorphic )
                {
                    part.Append( """
                            const r = new Map();
                            for( const i of o ) {
                                r.set( i[0], CTSType.fromTypedJson(i[1]) );
                            }
                            return r;
                            """ );
                }
                else
                {
                    part.Append( "const t = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( vType.NonNullable ) ).Append( "];" ).NewLine()
                        .Append( """
                                const r = new Map();
                                for( const i of o ) {
                                    r.set( i[0], t.nosj(i[1]) );
                                }
                                return r;
                                """ );
                }
                part.CloseBlock();
                if( vType.IsPolymorphic )
                {
                    part.Append( """
                            const r = new Map();
                            for( const p in o ) {
                                r.set( p, CTSType.fromTypedJson(o[p]) );
                            }
                            return r;
                            """ );
                }
                else
                {
                    part.Append( "const t = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( vType.NonNullable ) ).Append( "];" ).NewLine()
                        .Append( """
                                const r = new Map();
                                for( const p in o ) {
                                    r.set( p, t.nosj(o[p]) );
                                }
                                return r;
                                """ );
                }

            }
            else
            {
                part.Append( "if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );" ).NewLine();
                Throw.DebugAssert( "Dictionary key invariants.",
                                    c.ItemTypes[0].IsReadOnlyCompliant && !c.ItemTypes[0].IsNullable && !c.ItemTypes[0].IsPolymorphic );
                string aKey = GetMapAccessor( _jsonExhangeableNames, _requiresHandlingMap, part, c, 0 );
                string aVal = GetMapAccessor( _jsonExhangeableNames, _requiresHandlingMap, part, c, 1 );

                part.Append( $$"""
                        const r = new Map();
                        for( const i of o ) {
                            r.set({{aKey}},{{aVal}});
                        }
                        return r;
                        """ );
            }
        }
        part.NewLine().Append( "}," ).NewLine();

        static string GetMapAccessor( IPocoTypeNameMap jsonNames, JsonRequiresHandlingMap requiresMap, ITSKeyedCodePart part, ICollectionPocoType c, int i )
        {
            var vMap = $"i[{i}]";
            var t = c.ItemTypes[i];
            if( t.IsPolymorphic )
            {
                vMap = $"CTSType.fromTypedJson({vMap})";
            }
            else if( requiresMap.Contains( t ) )
            {
                part.Append( $"const t{i} = CTSType[" ).AppendSourceString( jsonNames.GetName( c.ItemTypes[i].NonNullable ) ).Append( "];" ).NewLine();
                vMap = $"t{i}.nosj({vMap})";
            }
            return vMap;
        }
    }

    void GenerateCompositeNosjFunction( ITSKeyedCodePart part, ITSType tsType, IPocoType t, IEnumerable<TSField> fields )
    {
        Throw.DebugAssert( t.Kind is PocoTypeKind.Record or PocoTypeKind.PrimaryPoco );
        part.Append( "nosj( o: any ) {" );
        part.Append( "if( o == null ) return undefined;" ).NewLine()
            .Append( "return new " ).Append( tsType.TypeName ).Append( "(" ).NewLine();
        bool atLeastOne = false;
        foreach( var field in fields )
        {
            if( atLeastOne ) part.Append( "," ).NewLine();
            atLeastOne = true;
            if( field.PocoField.Type.IsPolymorphic )
            {
                part.Append( "CTSType.fromTypedJson( o." ).Append( field.FieldName ).Append( " )" );
            }
            else
            {
                part.Append( $"CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( PocoCodeGenerator.MapType( field.PocoField.Type ).NonNullable ) )
                    .Append( "].nosj( " ).Append( "o." ).Append( field.FieldName ).Append( " )" );
            }
            if( t.Kind == PocoTypeKind.Record && field.HasNonNullDefault )
            {
                part.Append( " ?? " );
                field.WriteDefaultValue( part );
            }
        }
        part.Append( " );" ).NewLine();
        part.Append( "}," ).NewLine();
    }

    void GenerateAnonymousRecordNosjFunction( ITSKeyedCodePart part, IEnumerable<TSField> fields, bool useTupleSyntax )
    {
        part.Append( "nosj( o: any ) {" );
        part.Append( "if( o == null ) return undefined;" ).NewLine()
            .Append( "return " ).Append( useTupleSyntax ? "[" : "{" ).NewLine();
        int i = 0;
        foreach( var field in fields )
        {
            if( i != 0 ) part.Append( "," ).NewLine();
            if( !useTupleSyntax )
            {
                part.Append( _typeScriptContext.Root.ToIdentifier( field.PocoField.Name ) ).Append( ": " );
            }
            var tF = PocoCodeGenerator.MapType( field.PocoField.Type ).NonNullable;
            if( tF.IsPolymorphic )
            {
                part.Append( "CTSType.fromTypedJson( o[" ).Append( i ).Append( "] )" );
            }
            else 
            {
                part.Append( $"CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( tF ) ).Append( "].nosj( o[" )
                    .Append( i ).Append( "] )" );
            }
            if( field.HasNonNullDefault )
            {
                part.Append( " ?? " );
                field.WriteDefaultValue( part );
            }
            ++i;
        }
        part.Append( useTupleSyntax ? "];" : "};" ).NewLine()
            .Append("},").NewLine();
    }
}
