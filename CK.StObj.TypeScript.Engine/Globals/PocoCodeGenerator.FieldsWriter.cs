using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace CK.StObj.TypeScript.Engine
{

    partial class PocoCodeGenerator
    {
        readonly struct FieldsWriter
        {
            readonly TSField[] _fields;
            readonly ICompositePocoType _type;
            readonly TypeScriptRoot _root;
            readonly int _lastNonNullable;
            readonly int _lastWithNonNullDefault;
            readonly bool _useTupleSyntax;
            readonly bool _hasDefault;

            public bool HasDefault => _hasDefault;

            public ICompositePocoType Type => _type;

            public TypeScriptRoot Root => _root;

            FieldsWriter( ICompositePocoType type,
                          TSField[] fields,
                          TypeScriptRoot root,
                          int lastNonNullable,
                          int lastWithNonNullDefault,
                          bool useTupleSyntax,
                          bool hasDefault )
            {
                _type = type;
                _fields = fields;
                _root = root;
                _lastNonNullable = lastNonNullable;
                _lastWithNonNullDefault = lastWithNonNullDefault;
                _useTupleSyntax = useTupleSyntax;
                _hasDefault = hasDefault;
            }

            public static FieldsWriter Create( IActivityMonitor monitor,
                                               ICompositePocoType type,
                                               bool isAnonymousRecord,
                                               TypeScriptRoot root )
            {
                Throw.DebugAssert( isAnonymousRecord == (type is IRecordPocoType r && r.IsAnonymous) );
                var fields = type.Fields.Where( f => f.IsExchangeable )
                                        .Select( f => TSField.Create( monitor,
                                                                      root,
                                                                      f,
                                                                      root.TSTypes.ResolveTSType( monitor, f.Type ) ) )
                                        .ToArray();
                // Let's check a basic invariant.
                Throw.DebugAssert( fields.All( f => f.Field.Type.IsNullable == f.TSFieldType.IsNullable ) );

                // We use tuple syntax if all fields are unnamed. This applies to record only.
                bool useTupleSyntax = isAnonymousRecord;
                // We have a default record if all exchangeable fields have a default.
                // This applies to record only (a IPoco has always a default).
                bool hasDefault = true;

                // After lastNonNullable:
                //  - For record [tuple] syntax we can use "type?" but before we must use "type|undefined".
                //  - For constructor code, optional fields before this one must be moved after.
                int lastNonNullable = -1;

                // For record [tuple] syntax it is useless to add more ", undefined" after this one.
                int lastWithNonNullDefault = -1;

                for( int i = 0; i < fields.Length; i++ )
                {
                    ref var f = ref fields[i];
                    hasDefault &= f.HasDefault;
                    if( useTupleSyntax && !((IRecordPocoField)f.Field).IsUnnamed )
                    {
                        useTupleSyntax = false;
                    }
                    if( !f.IsNullable )
                    {
                        lastNonNullable = i;
                    }
                    if( f.HasNonNullDefault )
                    {
                        lastWithNonNullDefault = i;
                    }
                }
                return new FieldsWriter( type, fields, root, lastNonNullable, lastWithNonNullDefault, useTupleSyntax, hasDefault );
            }

            public TSType CreateAnonymousRecordType( IActivityMonitor monitor )
            {
                var typeBuilder = _root.GetTSTypeBuilder();
                if( _useTupleSyntax )
                {
                    bool atLeastOne = false;
                    typeBuilder.TypeName.Append( "[" );
                    if( _hasDefault ) typeBuilder.DefaultValue.Append( "[" );
                    for( int i = 0; i < _fields.Length; i++ )
                    {
                        ref TSField f = ref _fields[i];
                        if( atLeastOne )
                        {
                            typeBuilder.TypeName.Append( ", " );
                            if( i <= _lastWithNonNullDefault ) typeBuilder.DefaultValue.Append( ", " );
                        }
                        atLeastOne = true;
                        typeBuilder.TypeName.AppendTypeName( f.TSFieldType, useOptionalTypeName: i > _lastNonNullable );
                        if( _hasDefault && i <= _lastWithNonNullDefault ) f.WriteDefaultValue( typeBuilder.DefaultValue );
                    }
                    typeBuilder.TypeName.Append( "]" );
                    if( _hasDefault ) typeBuilder.DefaultValue.Append( "]" );
                }
                else
                {
                    typeBuilder.TypeName.Append( "{" );
                    typeBuilder.DefaultValue.Append( "{" );
                    bool atLeastOne = false;
                    bool atLeastOneDefault = false;
                    for( int i = 0; i < _fields.Length; i++ )
                    {
                        ref TSField f = ref _fields[i];
                        if( atLeastOne ) typeBuilder.TypeName.Append( ", " );
                        atLeastOne = true;
                        typeBuilder.TypeName.AppendIdentifier( f.Field.Name );
                        typeBuilder.TypeName.Append( f.IsNullable ? "?: " : ": " );
                        typeBuilder.TypeName.AppendTypeName( f.TSFieldType.NonNullable );
                        if( f.HasNonNullDefault )
                        {
                            if( atLeastOneDefault ) typeBuilder.DefaultValue.Append( ", " );
                            typeBuilder.DefaultValue.AppendIdentifier( f.Field.Name ).Append( ": " );
                            f.WriteDefaultValue( typeBuilder.DefaultValue );
                            atLeastOneDefault = true;
                        }
                    }
                    typeBuilder.TypeName.Append( "}" );
                    typeBuilder.DefaultValue.Append( "}" );
                }
                return typeBuilder.Build();
            }

            public bool GenerateRecordType( IActivityMonitor monitor, TypeScriptContext typeScriptContext, IRecordPocoType type, ITSGeneratedType tsType )
            {
                Throw.DebugAssert( _type is IRecordPocoType r && !r.IsAnonymous );
                var part = CreateTypePart( monitor, tsType );
                if( part == null ) return false;
                var root = typeScriptContext.Root;
                if( root.DocBuilder.GenerateDocumentation )
                {
                    var xE = XmlDocumentationReader.GetDocumentationFor( monitor, tsType.Type, root.Memory );
                    if( xE != null )
                    {
                        if( xE.Elements( "param" ).Any() )
                        {
                            // Clones the element and removes any param
                            // elements from it (for record constructor syntax).
                            xE = new XElement( xE );
                            xE.Elements( "param" ).Remove();
                        }
                        part.AppendDocumentation( xE );
                    }
                }
                // INamedRecord is a pure TS type defined in IPoco.ts.
                tsType.File.Imports.EnsureImport( typeScriptContext.GetTypeScriptPocoType( monitor ).File, "INamedRecord" );
                part.Append( "export class " ).Append( tsType.TypeName ).Append( " implements INamedRecord")
                    .OpenBlock()
                    .Append( "constructor( " ).NewLine();
                // A record constructor can have required parameters (not nullable, no default):
                // they come first.
                SortCtorParameters();
                for( int i = 0; i < _fields.Length; i++ )
                {
                    if( i > 0 )
                    {
                        part.Append( ", " ).NewLine();
                    }
                    _fields[i].WriteCtorFieldDefinition( tsType.File, part );
                }
                part.NewLine().Append( ") {}" ).NewLine()
                    // The get pocoTypeModel() returns a static (shared pocoTypeModel instance).
                    .Append( "get pocoTypeModel() { return " )
                    .Append( tsType.TypeName ).Append( "._m; }" ).NewLine()
                    // The pocoTypeModel is extensible. 
                    .Append( "private static readonly _m = {" ).NewLine()
                    .Append( "isNamedRecord: true," ).NewLine()
                    .CreatePart( out var pocoTypeModelPart )
                    .Append( "};" ).NewLine();
                WritePocoTypeModel(monitor, pocoTypeModelPart, type );
                part.Append( "readonly _brand!: INamedRecord[\"_brand\"] & {\"" )
                    .Append( (type.Index >> 1).ToString( CultureInfo.InvariantCulture ) ).Append( "\":any};" ).NewLine();
                return true;
            }

            public TSPocoField[] GetPocoFields()
            {
                SortCtorParameters();
                var fields = new TSPocoField[_fields.Length];
                for( int i = 0; i < _fields.Length; i++ )
                {
                    fields[i] = new TSPocoField( _fields[i] );
                }
                return fields;
            }

            /// <summary>
            /// For records (Pocos have all defaults by design), we may not have a default value
            /// for each fields: they are required parameters. We move them at the beginning of the array.
            /// <para>
            /// We reorder all the fields based on 4 categories:
            /// <list type="number">
            ///   <item>Non nullable, no default (the required).</item>
            ///   <item>Non Nullable with default.</item>
            ///   <item>Nullable with non null default.</item>
            ///   <item>Nullable without default (the optionals).</item>
            /// </list>
            /// </para>
            /// </summary>
            public void SortCtorParameters()
            {
                int requiredOffset = 0;
                int nonNullableWithDefaultOffset = 0;
                int nullableWithDefaultOffset = 0;
                for( int i = 0; i < _fields.Length; i++ )
                {
                    var field = _fields[i];
                    if( !field.HasDefault )
                    {
                        if( i > requiredOffset )
                        {
                            MoveUp( _fields, i, requiredOffset );
                        }
                        requiredOffset++;
                        nonNullableWithDefaultOffset++;
                        nullableWithDefaultOffset++;
                    }
                    else if( !field.IsNullable )
                    {
                        if( i > nonNullableWithDefaultOffset )
                        {
                            MoveUp( _fields, i, nonNullableWithDefaultOffset );
                        }
                        nonNullableWithDefaultOffset++;
                        nullableWithDefaultOffset++;
                    }
                    else if( field.HasNonNullDefault )
                    {
                        if( i > nullableWithDefaultOffset )
                        {
                            MoveUp( _fields, i, nullableWithDefaultOffset );
                        }
                        nullableWithDefaultOffset++;
                    }
                }

                static void MoveUp( TSField[] fields, int i, int j )
                {
                    var f = fields[i];
                    Array.Copy( fields, j, fields, j + 1, i - j );
                    fields[j] = f;
                }
            }

            internal void WritePocoTypeModel( IActivityMonitor monitor, ITSCodePart pocoTypeModelPart, ICompositePocoType t )
            {
                pocoTypeModelPart.Append( "name: " ).AppendSourceString( t.ExternalOrCSharpName ).Append( "," ).NewLine()
                .Append( "idxName: \"" ).Append( (t.Index >> 1).ToString( CultureInfo.InvariantCulture ) ).Append( "\"," );
                // Let the trailing comma appear even if no one add content to pocoTypeModelPart.
            }
        }

    }
}
