using CK.Core;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
        readonly ITSCodePart _ctsType;
        readonly RequiresHandlingMap _requiresHandlingMap;

        internal CTSTypeSystem( TypeScriptContext typeScriptContext, IPocoTypeNameMap jsonExhangeableNames )
        {
            _typeScriptContext = typeScriptContext;
            _jsonExhangeableNames = jsonExhangeableNames;
            _requiresHandlingMap = new RequiresHandlingMap( jsonExhangeableNames.TypeSet );
            var file = _typeScriptContext.Root.Root.FindOrCreateFile( "CK/Core/CTSType.ts" );
            file.Imports.EnsureImport( typeScriptContext.Root.TSTypes.TSTypeFile, "TSType" );

            _ctsType = file.Body.Append( """export const SymCTS = Symbol.for("CK.CTSType");""" ).NewLine()
                                .Append( """
                                export const CTSType: any  = {
                                   typedJson( o: any ) : unknown {
                                     if( o === null || typeof o === "undefined" ) return null;
                                     const t = o[SymCTS];
                                     if( !t ) throw new Error( "Untyped object. A type must be specified with CTSType." );
                                     return [t.name, t.json( o )];
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
                    var ctorBody = ts?.TypePart.FindKeyedPart( ITSKeyedCodePart.ConstructorBodyPart );
                    if( ctorBody != null )
                    {
                        ctorBody.File.Imports.EnsureImport( _ctsType.File, "CTSType" );
                        ctorBody.Append( "CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( t ) ).Append( "].set( this );" ).NewLine();
                    }
                    else
                    {
                        e.Monitor.Warn( $"ConstructorBodyPart not found type '{t}'. " +
                                        $"This type should have been generated in a file and its constructor should contain a \"ConstructorBody\" part." );
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
        internal ITSCodePart EnsureMapping( IPocoType t, ITSType ts )
        {
            Throw.DebugAssert( !t.IsNullable && !ts.IsNullable );
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

                    part.Append( "json( o: any ) {" ).NewLine();
                    if( c.Kind is PocoTypeKind.Array or PocoTypeKind.List )
                    {
                        if( !_requiresHandlingMap.Contains( t ) )
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
                        if( !_requiresHandlingMap.Contains( t ) )
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
                        if( c.ItemTypes[0].Type == typeof( string ) )
                        {
                            if( !_requiresHandlingMap.Contains( c.ItemTypes[1] ) )
                            {
                                part.Append( "return !!o ? Object.fromEntries(o.entries()) : null;" );
                            }
                            else if( c.ItemTypes[1].IsPolymorphic )
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
                                    .Append( "const t = CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( c.ItemTypes[1].NonNullable ) ).Append( "];" ).NewLine()
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
                            if( !_requiresHandlingMap.Contains( t ) )
                            {
                                part.Append( "return !!o ? Array.from( o ) : null;" );
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
                else if( ts is TSUnionType tU )
                {
                    part.Append( "allowedTypes: [ " );
                    WriteItemTypes( part, tU.Types.Select( uT => uT.PocoType ) );
                    part.Append( " ]," ).NewLine();
                }
                else if( t.Kind == PocoTypeKind.Basic )
                {
                    part.Append( "json( o: any ) {" );
                    if( _requiresHandlingMap.RequiresToJSONCall( t ) )
                    {
                        part.Append( "return !!o ? o.toJSON() : null;" );
                    }
                    else if( t.Type == typeof( TimeSpan ) )
                    {
                        // We do what we can here. In a perfect world, toMillis() should return a BigInt instead
                        // of a Number. We simply add '0000' to "convert" into 10th of microseconds (100 nanoseconds).
                        part.Append( "return o.toMillis().toString()+'0000';" );
                    }
                    else if( t.Type == typeof( long ) || t.Type == typeof( ulong ) || t.Type == typeof( BigInteger ) || t.Type == typeof( long )
                             || t.Type == typeof(decimal) )
                    {
                        // These are mapped to BigInt and this primitive type has no toJson support.
                        // See https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt#use_within_json.
                        // The toString() method is fine for us.
                        // And this MUST also work for Decimal implementation: both https://mikemcl.github.io/decimal.js-light/#toString
                        // and https://mikemcl.github.io/decimal.js/#toString do the job.
                        part.Append( "return o.toString();" );
                    }
                    else
                    {
                        // MCString and CodeString are excluded from TypeScript exchangeable set.
                        // We are left with the primitive types.
                        if( !ts.IsPrimitive ) Throw.CKException( $"Unsuported Basic type '{t.Type}'." );
                        part.Append( "return o;" );
                    }
                    part.NewLine().Append( "}," ).NewLine();
                }
                else if( t.Kind == PocoTypeKind.Enum )
                {
                    part.Append( "json( o: any ) { return o; }" ).NewLine();
                }
            }
            return part;
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
            DoWriteFieldModels( part, fields );
        }

        internal void OnGeneratingNamedRecord( GeneratingNamedRecordPocoEventArgs e )
        {
            WriteFieldModels( e.RecordPocoType, e.Fields.Select( f => f.TSField ) );
        }

        internal void OnGeneratingPrimaryPoco( GeneratingPrimaryPocoEventArgs e )
        {
            WriteFieldModels( e.PrimaryPocoType, e.Fields.Select( f => f.TSField ) );
        }

        void WriteFieldModels( IPocoType t, IEnumerable<TSField> fields )
        {
            Throw.DebugAssert( !t.IsNullable );
            var ctsName = _jsonExhangeableNames.GetName( t );
            var part = _ctsType.FindKeyedPart( ctsName );
            Throw.DebugAssert( part != null );
            DoWriteFieldModels( part, fields );

            // SimpleUserMessage uses type toJSON method.
            if( t.Type == typeof( SimpleUserMessage ) ) return;

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
                        part.Append( "CTSType.typedJson( o." ).AppendIdentifier( field.PocoField.Name ).Append(" )" );
                    }
                    else if( _requiresHandlingMap.Contains( field.PocoField.Type ) )
                    {
                        part.Append( $"CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( field.PocoField.Type.NonNullable ) ).Append( "].json( " )
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

        void DoWriteFieldModels( ITSCodePart part, IEnumerable<TSField> fields )
        {
            part.Append( "fields: {" );
            int i = 0;
            foreach( var f in fields )
            {
                if( i > 0 ) part.Append( "," );
                part.NewLine()
                    .AppendIdentifier( f.PocoField.Name ).Append( ": {" ).NewLine()
                    .Append( "name: '" ).AppendIdentifier( f.PocoField.Name ).Append( "'," ).NewLine();
                WriteIsNullableAndType( part, f.PocoField.Type, isField: true );
                part.Append( "}" ).NewLine();
                ++i;
            }
            part.NewLine().Append( "}," ).NewLine();
        }

    }

}
