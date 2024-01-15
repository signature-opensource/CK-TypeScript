using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace CK.StObj.TypeScript.Engine
{
    public partial class PocoCodeGenerator
    {
        readonly struct FieldsWriter
        {
            /// <summary>
            /// Captures field information. One cannot capture the default value here since
            /// we may need to write it when a field has its own default.
            /// We use the TsFieldType.DefaultValueSource to detect if a default value is available
            /// instead of (Field.DefaultValueInfo.IsDisallowed is false): this allows TypeScript
            /// default to exist even if there is no default for the C#... and because we consider 
            /// only exchangeable fields, this may save some applicable defaults.
            /// </summary>
            readonly record struct TSField
            {
                public readonly IPocoField Field;
                public readonly ITSType TSFieldType;
                public readonly ImmutableArray<XElement> Docs;
                public readonly bool HasDefault;
                public readonly bool HasNonNullDefault;

                TSField( IPocoField field, ITSType tsFieldType, bool hasDefault, bool hasNonNullDefault, ImmutableArray<XElement> doc )
                {
                    Field = field;
                    TSFieldType = tsFieldType;
                    HasDefault = hasDefault;
                    HasNonNullDefault = hasNonNullDefault;
                    Docs = doc;
                }

                public bool IsNullable => TSFieldType.IsNullable;
                 
                public static TSField Create( IActivityMonitor monitor, TypeScriptRoot root, IPocoField field, ITSType tsFieldType )
                {
                    var doc = GetDocumentation( monitor, root, field.Originator );
                    return new TSField( field,
                                        tsFieldType,
                                        hasDefault: field.HasOwnDefaultValue || tsFieldType.DefaultValueSource != null,
                                        hasNonNullDefault: field.HasOwnDefaultValue || !tsFieldType.IsNullable,
                                        doc );
                }

                static ImmutableArray<XElement> GetDocumentation( IActivityMonitor monitor, TypeScriptRoot root, object? originator )
                {
                    if( !root.DocBuilder.GenerateDocumentation )
                    {
                        return ImmutableArray<XElement>.Empty;
                    }
                    switch( originator )
                    {
                        case null:
                            return ImmutableArray<XElement>.Empty;
                        case IPocoPropertyInfo p:
                                return XmlDocumentationReader.GetDocumentationFor( monitor, p.DeclaredProperties.Select( i => i.PropertyInfo ), root.Memory )
                                                             .ToImmutableArray();
                        case MemberInfo m:
                            var d = XmlDocumentationReader.GetDocumentationFor( monitor, m, root.Memory );
                            return d != null ? ImmutableArray.Create( d ) : ImmutableArray<XElement>.Empty;
                        case ParameterInfo p:
                            var dM = XmlDocumentationReader.GetDocumentationFor( monitor, p.Member, root.Memory );
                            var dP = dM?.Elements( "param" ).FirstOrDefault( e => p.Name == e.Attribute("name" )?.Value );
                            if( dP == null ) return ImmutableArray<XElement>.Empty;
                            var summary = new XElement( "summary", dP.Nodes() );
                            // The outer name can be anything.
                            return ImmutableArray.Create( new XElement( summary.Name, summary ) );
                        default: return Throw.NotSupportedException<ImmutableArray<XElement>>();
                    }
                }
            }

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
                        if( _hasDefault && i <= _lastWithNonNullDefault ) WriteDefaultValue( monitor, typeBuilder.DefaultValue, ref f );
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
                            WriteDefaultValue( monitor, typeBuilder.DefaultValue, ref f );
                            atLeastOneDefault = true;
                        }
                    }
                    typeBuilder.TypeName.Append( "}" );
                    typeBuilder.DefaultValue.Append( "}" );
                }
                return typeBuilder.Build();
            }

            public bool GenerateRecordType( IActivityMonitor monitor, ITSGeneratedType tsType )
            {
                Throw.DebugAssert( _type is IRecordPocoType r && !r.IsAnonymous );
                var part = CreateTypePart( monitor, tsType );
                if( part == null ) return false;
                var root = tsType.File.Root;
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
                part.Append( "export class " ).Append( tsType.TypeName )
                    .OpenBlock()
                    .Append( "constructor( " ).NewLine();
                WriteCtorParameters( monitor, tsType.File, part );
                part.NewLine().Append( ") {}" );
                return true;
            }

            static void WriteDefaultValue( IActivityMonitor monitor, ITSCodeWriter w, ref TSField f )
            {
                Throw.DebugAssert( f.HasDefault );
                var defInfo = f.Field.DefaultValueInfo;
                var defVal = defInfo.RequiresInit ? defInfo.DefaultValue.SimpleValue : null;
                if( defVal != null )
                {
                    f.TSFieldType.WriteValue( w, defVal );
                }
                else
                {
                    // Even if RequiresInit is true, the SimpleValue object can be null
                    // for complex objects: use the type's default value.
                    w.Append( f.TSFieldType.DefaultValueSource );
                }
            }

            public void WriteCtorParameters( IActivityMonitor monitor, TypeScriptFile file, ITSCodeWriter w )
            {
                // If we don't have a default, it's because one or more fields have no
                // default value: they are required. We move them at the beginning of the array.
                // Same for the other "type" of fields... The order is:
                // - Non nullable, no default (the required).
                // - Non Nullable with default.
                // - Nullable with non null default.
                // - Nullable without default (the optionals).
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

                for( int i = 0; i < _fields.Length; i++ )
                {
                    if( i > 0 )
                    {
                        w.Append( ", " ).NewLine();
                    }
                    WriteFieldDefinition( monitor, file, w, ref _fields[i] );
                }

                static void MoveUp( TSField[] fields, int i, int j )
                {
                    var f = fields[i];
                    Array.Copy( fields, j, fields, j+1, i-j );
                    fields[j] = f;
                }

                static void WriteFieldDefinition( IActivityMonitor monitor, TypeScriptFile file, ITSCodeWriter w, ref TSField f )
                {
                    using( file.Root.DocBuilder.RemoveGetOrSetPrefix() )
                    {
                        w.AppendDocumentation( f.Docs );
                    }
                    w.Append( "public " );
                    bool ro = f.Field is IPrimaryPocoField pF && pF.FieldAccess is PocoFieldAccessKind.MutableReference or PocoFieldAccessKind.IsByRef;
                    if( ro ) w.Append( "readonly " );
                    w.AppendIdentifier( f.Field.Name );
                    bool optField = f.IsNullable && !f.HasNonNullDefault;
                    if( optField ) w.Append( "?" );
                    w.Append( ": " ).AppendTypeName( optField ? f.TSFieldType.NonNullable : f.TSFieldType );
                    if( f.HasNonNullDefault )
                    {
                        w.Append( " = " );
                        WriteDefaultValue( monitor, w, ref f );
                    }
                }
            }

        }

    }
}
