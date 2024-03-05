using CK.Core;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;

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
        readonly HasPolymorphicMap _hasPolymorphicMap;

        internal CTSTypeSystem( TypeScriptContext typeScriptContext, IPocoTypeNameMap jsonExhangeableNames )
        {
            _typeScriptContext = typeScriptContext;
            _jsonExhangeableNames = jsonExhangeableNames;
            _hasPolymorphicMap = new HasPolymorphicMap( jsonExhangeableNames.TypeSet );
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
                if( t.IsOblivious && t.Kind is PocoTypeKind.Basic or PocoTypeKind.Record or PocoTypeKind.PrimaryPoco )
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
            Throw.DebugAssert( t.IsNullable == ts.IsNullable );
            Throw.DebugAssert( t.IsOblivious );
            var ctsName = _jsonExhangeableNames.GetName( t );
            var part = _ctsType.FindOrCreateKeyedPart( ctsName, closer: "},\n" );
            if( part.IsEmpty )
            {
                part.AppendSourceString( ctsName ).Append( ": {" ).NewLine()
                    .Append( "name: " ).AppendSourceString( ctsName ).Append( "," ).NewLine()
                    .Append( "tsType: TSType[" ).AppendSourceString( ts.TypeName ).Append( "]," ).NewLine();
                var isAbstract = t.Kind == PocoTypeKind.Any || t.Kind == PocoTypeKind.AbstractPoco || t.Kind == PocoTypeKind.UnionType;
                if( !isAbstract )
                {
                    part.File.Imports.EnsureImport( ts );
                    part.Append( "set( o: " ).Append( ts.TypeName ).Append( " ): " ).Append( ts.TypeName )
                        .Append( " { (o as any)[SymCTS] = this; return o; }," ).NewLine();
                }
                if( t is ICollectionPocoType c )
                {
                    part.Append( "itemTypes: [ " );
                    WriteItemTypes( part, c.ItemTypes );
                    part.Append( " ]," ).NewLine();

                    part.Append( "json( o: any ) {" ).NewLine();
                    if( c.Kind is PocoTypeKind.Array or PocoTypeKind.List )
                    {
                        if( !_hasPolymorphicMap.Contains( t ) )
                        {
                            part.Append( "return o;" );
                        }
                        else
                        {
                            if( c.ItemTypes[0].IsPolymorphic )
                            {
                                part.Append( "return o.map( CTSType.typedJson );" );
                            }
                            else
                            {
                                part.Append( "const t = this[" ).AppendSourceString( _jsonExhangeableNames.GetName( c.ItemTypes[0] ) ).Append( "];" ).NewLine();
                                part.Append( "return o.map( t.json );" );
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
                    part.Append( "json( o: any ) { return o.toJSON(); }," ).NewLine();
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
                .Append( "CTSType[" ).AppendSourceString( _jsonExhangeableNames.GetName( t.NonNullable.ObliviousType ) )
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
            var ctsName = _jsonExhangeableNames.GetName( t );
            var part = _ctsType.FindKeyedPart( ctsName );
            Throw.DebugAssert( part != null );
            DoWriteFieldModels( part, fields );
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
            part.NewLine().Append( "}" ).NewLine();
        }

    }

}
