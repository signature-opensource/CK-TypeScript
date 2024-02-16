using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace CK.StObj.TypeScript.Engine
{

    /// <summary>
    /// Captures field information.
    /// </summary>
    /// <remarks>
    /// One cannot capture the default value here since we may need to write it when a field has its own default
    /// with <see cref="ITSType.TryWriteValue(ITSCodeWriter, object?)"/>.
    /// We use the <see cref="ITSType.DefaultValueSource"/> to detect if a default value is available
    /// instead of <see cref="IPocoField.DefaultValueInfo"/>.<see cref="DefaultValueInfo.IsDisallowed"/> == false:
    /// this allows TypeScript default to exist even if there is no default for the C#... and because we consider 
    /// only exchangeable fields, this may save some applicable defaults.
    /// </remarks>
    public readonly struct TSField
    {
        /// <summary>
        /// Gets the Poco field.
        /// </summary>
        public readonly IPocoField PocoField;

        /// <summary>
        /// Gets the TypeScript field type.
        /// </summary>
        public readonly ITSType TSFieldType;

        /// <summary>
        /// Gets the field documentation elements.
        /// </summary>
        public readonly ImmutableArray<XElement> Docs;

        /// <summary>
        /// Gets whether this field is nullable. 
        /// </summary>
        public bool IsNullable => TSFieldType.IsNullable;

        /// <summary>
        /// Gets whether this field has a default value (including "undefined").
        /// </summary>
        public bool HasDefault => PocoField.HasOwnDefaultValue || TSFieldType.DefaultValueSource != null;

        /// <summary>
        /// Gets whether this field has a non null (not "undefined" for TypeScript) default.
        /// </summary>
        public bool HasNonNullDefault => (PocoField.DefaultValueInfo.RequiresInit && PocoField.DefaultValueInfo.DefaultValue.SimpleValue != null)
                                            || TSFieldType.DefaultValueSource is not null and not "undefined";

        TSField( IPocoField field, ITSType tsFieldType, ImmutableArray<XElement> doc )
        {
            PocoField = field;
            TSFieldType = tsFieldType;
            Docs = doc;
        }

        internal static TSField Create( IActivityMonitor monitor, TypeScriptContext context, IPocoField field, ITSType tsFieldType )
        {
            var doc = GetDocumentation( monitor, context.Root, field.Originator );
            return new TSField( field, tsFieldType, doc );

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
                        var dP = dM?.Elements( "param" ).FirstOrDefault( e => p.Name == e.Attribute( "name" )?.Value );
                        if( dP == null ) return ImmutableArray<XElement>.Empty;
                        var summary = new XElement( "summary", dP.Nodes() );
                        // The outer name can be anything.
                        return ImmutableArray.Create( new XElement( summary.Name, summary ) );
                    default: return Throw.NotSupportedException<ImmutableArray<XElement>>();
                }
            }
        }

        internal readonly void WriteCtorFieldDefinition( TypeScriptFile file,
                                                            ITSCodeWriter w,
                                                            IEnumerable<XElement>? docs = null,
                                                            Action<DocumentationBuilder>? extension = null )
        {
            using( file.Root.DocBuilder.RemoveGetOrSetPrefix() )
            {
                w.AppendDocumentation( docs ?? Docs, extension );
            }
            w.Append( "public " );
            bool ro = PocoField is IPrimaryPocoField pF && pF.FieldAccess is PocoFieldAccessKind.MutableReference or PocoFieldAccessKind.IsByRef;
            if( ro ) w.Append( "readonly " );
            w.AppendIdentifier( PocoField.Name );
            bool optField = IsNullable && !HasNonNullDefault;
            if( optField ) w.Append( "?" );
            w.Append( ": " ).AppendTypeName( optField ? TSFieldType.NonNullable : TSFieldType );
            if( HasNonNullDefault )
            {
                w.Append( " = " );
                WriteDefaultValue( w );
            }
        }

        internal readonly void WriteDefaultValue( ITSCodeWriter w )
        {
            Throw.DebugAssert( HasDefault );
            var defInfo = PocoField.DefaultValueInfo;
            var defVal = defInfo.RequiresInit ? defInfo.DefaultValue.SimpleValue : null;
            if( defVal != null )
            {
                TSFieldType.WriteValue( w, defVal );
            }
            else
            {
                // Even if RequiresInit is true, the SimpleValue object can be null
                // for complex objects: use the type's default value.
                w.Append( TSFieldType.DefaultValueSource );
            }
        }
    }
}
