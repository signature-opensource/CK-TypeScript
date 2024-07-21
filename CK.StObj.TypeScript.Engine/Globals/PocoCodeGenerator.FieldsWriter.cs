using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;

namespace CK.StObj.TypeScript.Engine
{

    partial class PocoCodeGenerator
    {
        internal readonly struct FieldsWriter
        {
            readonly TSField[] _fields;
            readonly TypeScriptContext _typeScriptContext;
            readonly int _lastNonNullable;
            readonly int _lastWithNonNullDefault;
            readonly bool _useTupleSyntax;
            readonly bool _hasDefault;

            public bool HasDefault => _hasDefault;

            FieldsWriter( TSField[] fields,
                          TypeScriptContext context,
                          int lastNonNullable,
                          int lastWithNonNullDefault,
                          bool useTupleSyntax,
                          bool hasDefault )
            {
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
                                               TypeScriptContext context,
                                               IPocoTypeSet exchangeableSet )
            {
                Throw.DebugAssert( isAnonymousRecord == (type is IRecordPocoType r && r.IsAnonymous) );
                var fields = type.Fields.Where( f => exchangeableSet.Contains( f.Type ) )
                                        .Select( f => TSField.Create( monitor,
                                                                      context,
                                                                      f,
                                                                      context.Root.TSTypes.ResolveTSType( monitor, f.Type ) ) )
                                        .ToArray();
                // Let's check a basic invariant.
                Throw.DebugAssert( fields.All( f => f.PocoField.Type.IsNullable == f.TSFieldType.IsNullable ) );

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
                    if( useTupleSyntax && !((IRecordPocoField)f.PocoField).IsUnnamed )
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
                return new FieldsWriter( fields, context, lastNonNullable, lastWithNonNullDefault, useTupleSyntax, hasDefault );
            }

            /// <summary>
            /// Gets whether [tuple, syntax] iw used instead of {"tuple": "syntax"}.
            /// This is true only for anonymous records where all fields are <see cref="IRecordPocoField.IsUnnamed"/>.
            /// </summary>
            public bool UseTupleSyntax => _useTupleSyntax;

            public ITSType CreateAnonymousRecordType( IActivityMonitor monitor, out TSField[] fields )
            {
                var typeBuilder = _typeScriptContext.Root.GetTSTypeSignatureBuilder();
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
                        typeBuilder.TypeName.Append( f.FieldName );
                        typeBuilder.TypeName.Append( f.IsNullable ? "?: " : ": " );
                        typeBuilder.TypeName.AppendTypeName( f.TSFieldType.NonNullable );
                        if( f.HasNonNullDefault )
                        {
                            if( atLeastOneDefault ) typeBuilder.DefaultValue.Append( ", " );
                            typeBuilder.DefaultValue.Append( f.FieldName ).Append( ": " );
                            f.WriteDefaultValue( typeBuilder.DefaultValue );
                            atLeastOneDefault = true;
                        }
                    }
                    typeBuilder.TypeName.Append( "}" );
                    typeBuilder.DefaultValue.Append( "}" );
                }
                fields = _fields;
                return typeBuilder.Build();
            }

            /// <summary>
            /// Used for PrimaryPoco: fields are not sorted.
            /// </summary>
            /// <returns>The fields.</returns>
            public ImmutableArray<TSNamedCompositeField> GetNamedCompositeFields()
            {
                return ImmutableArray.CreateRange( ImmutableCollectionsMarshal.AsImmutableArray( _fields ), f => new TSNamedCompositeField( f ) );
            }

            /// <summary>
            /// Used for Records: fields are sorted.
            /// </summary>
            /// <returns>The fields.</returns>
            public ImmutableArray<TSNamedCompositeField> SortAndGetNamedCompositeFields()
            {
                SortCtorParameters();
#if DEBUG
                TSField[] clone = (TSField[])_fields.Clone();
                SortCtorParameters();
                Throw.DebugAssert( "SortCtorParameters must be a stable sort (even if we call it only once).", clone.SequenceEqual( _fields ) );
#endif
                return GetNamedCompositeFields();
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
            void SortCtorParameters()
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
        }

    }
}
