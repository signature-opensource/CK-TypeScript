using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
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
            readonly TypeScriptContext _typeScriptContext;
            readonly int _lastNonNullable;
            readonly int _lastWithNonNullDefault;
            readonly bool _useTupleSyntax;
            readonly bool _hasDefault;

            public bool HasDefault => _hasDefault;

            FieldsWriter( ICompositePocoType type,
                          TSField[] fields,
                          TypeScriptContext context,
                          int lastNonNullable,
                          int lastWithNonNullDefault,
                          bool useTupleSyntax,
                          bool hasDefault )
            {
                _type = type;
                _fields = fields;
                _typeScriptContext = context;
                _lastNonNullable = lastNonNullable;
                _lastWithNonNullDefault = lastWithNonNullDefault;
                _useTupleSyntax = useTupleSyntax;
                _hasDefault = hasDefault;
            }

            public static FieldsWriter Create( IActivityMonitor monitor,
                                               ICompositePocoType type,
                                               bool isAnonymousRecord,
                                               TypeScriptContext context )
            {
                Throw.DebugAssert( isAnonymousRecord == (type is IRecordPocoType r && r.IsAnonymous) );
                var fields = type.Fields.Where( f => context.IsExchangeable( f.Type ) )
                                        .Select( f => TSField.Create( monitor,
                                                                      context,
                                                                      f,
                                                                      context.Root.TSTypes.ResolveTSType( monitor, f.Type ) ) )
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
                return new FieldsWriter( type, fields, context, lastNonNullable, lastWithNonNullDefault, useTupleSyntax, hasDefault );
            }

            public TSType CreateAnonymousRecordType( IActivityMonitor monitor )
            {
                var typeBuilder = _typeScriptContext.Root.GetTSTypeBuilder();
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

            public TSPocoField[] WritePocoTypeModelAndGetPocoFields( ITSCodePart part, ITSType tsType )
            {
                SortCtorParameters();
                // The get pocoTypeModel() returns a static (shared pocoTypeModel instance).
                part.Append( "get pocoTypeModel() { return " )
                    .Append( tsType.TypeName ).Append( "._m; }" ).NewLine()
                    // The pocoTypeModel is extensible. 
                    .Append( "private static readonly _m = {" ).NewLine()
                    .Append( "isNamedRecord: " ).Append( _type.Kind == PocoTypeKind.Record ).Append( "," ).NewLine()
                    // Type name is CSharpName, not INamedPocoType.ExternalOrCSharpName so that this name is coherent
                    // with FieldMode.csType that is bound to any IPocoType.
                    .Append( "type: " ).AppendSourceString( tsType.TypeName ).Append( "," ).NewLine()
                    .Append( "index: " ).Append( (_type.Index >> 1).ToString( CultureInfo.InvariantCulture ) ).Append( "," ).NewLine()
                    .Append( "fields: [" ).NewLine();

                var fields = new TSPocoField[_fields.Length];
                for( int i = 0; i < _fields.Length; i++ )
                {
                    if( i > 0 ) part.Append( "," );
                    var f = _fields[i];
                    Throw.DebugAssert( "This has been handled while building the array of fields.",
                                       _typeScriptContext.IsExchangeable( _fields[i].Field.Type ) );
                    part.OpenBlock()
                        .Append( "name: " ).AppendSourceString( f.Field.Name ).Append( "," ).NewLine()
                        .Append( "type: " ).AppendSourceString( f.TSFieldType.TypeName ).Append( "," ).NewLine()
                        .Append( "isOptional:" ).Append( f.IsNullable ).Append( "," ).NewLine()
                        .Append( "index:" ).Append( i.ToString( CultureInfo.InvariantCulture ) ).Append( "," ).NewLine();
                    // This creates the TSPocoField.FieldModelPart (with the '}' closer).
                    fields[i] = new TSPocoField( part, f );
                }
                // Let the trailing comma appear even if no one add content to pocoTypeModelPart.
                part.Append( "]," ).NewLine();
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
                .Append( "index: " ).Append( (t.Index >> 1).ToString( CultureInfo.InvariantCulture ) ).Append( "," );
                // Let the trailing comma appear even if no one add content to pocoTypeModelPart.
            }
        }

    }
}
