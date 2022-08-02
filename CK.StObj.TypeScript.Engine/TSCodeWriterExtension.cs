using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Diagnostics;
using CK.Setup;
using CK.Core;
using CK.StObj.TypeScript.Engine;
using CK.CodeGen;
using System.Numerics;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Provides Append fluent extension methods to <see cref="ITSCodeWriter"/> specializations.
    /// </summary>
    public static class TSCodeWriterExtensions
    {
        /// <summary>
        /// Appends an enum definition. The underlying type should be safely convertible into Int32.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="enumType">The enum type.</param>
        /// <param name="typeName">The TypeScript type name.</param>
        /// <param name="export">True to prefix with 'export ' the enum definition.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T AppendEnumDefinition<T>( this T @this, IActivityMonitor monitor, Type enumType, string typeName, bool export ) where T : ITSCodeWriter
        {
            Throw.CheckArgument( enumType.IsEnum );
            var uT = enumType.GetEnumUnderlyingType();
            if( uT == typeof(UInt32) || uT == typeof(Int64) || uT == typeof(UInt64) )
            {
                monitor.Error( $"Enum: {enumType.Name} cannot be generated as TypeScript since it is based on {uT.Name} type. Only types that can be safely converted into Int32 should be used." );
                return @this;
            }
            string? docValuePrefix = null;
            var xDoc = @this.File.Folder.Root.GenerateDocumentation
                        ? XmlDocumentationReader.GetXmlDocumentation( monitor, enumType.Assembly, @this.File.Folder.Root.Memory )
                        : null;
            if( xDoc != null )
            {
                @this.AppendDocumentation( xDoc, enumType );
                docValuePrefix = XmlDocumentationReader.GetNameAttributeValueFor( "F:", enumType );
            }
            return @this.Append( export ? "export enum " : "enum " ).Append( typeName )
                        .OpenBlock()
                        .Append( b =>
                        {
                            string[] names = Enum.GetNames( enumType );
                            int[] values = Enum.GetValues( enumType ).Cast<object>().Select( x => Convert.ToInt32( x ) ).ToArray();

                            for( int i = 0; i < names.Length; ++i )
                            {
                                if( i > 0 ) b.Append( "," ).NewLine();

                                var n = names[i];
                                if( xDoc != null )
                                {
                                    Debug.Assert( docValuePrefix != null );
                                    b.AppendDocumentation( XmlDocumentationReader.GetDocumentationElement( xDoc, docValuePrefix + '.' + n ) );
                                }
                                b.Append( n ).Append( " = " ).Append( values[i] );
                            }
                        } )
                        .CloseBlock();
        }

        /// <summary>
        /// Appends a type name defined in a <see cref="TSTypeFile"/>: the type is automatically
        /// imported from the file (in the <see cref="TypeScriptFile.Imports"/>).
        /// </summary>
        /// <typeparam name="T">The code writer type.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="t">The type to import.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        public static T AppendImportedTypeName<T>( this T @this, TSTypeFile t ) where T : ITSCodeWriter
        {
            @this.File.Imports.EnsureImport( t.File, t.TypeName );
            @this.Append( t.TypeName );
            return @this;
        }


        /// <summary>
        /// Calls <see cref="AppendComplexTypeName(ITSCodeWriter, IActivityMonitor, TypeScriptContext, NullableTypeTree, bool, bool)"/> and
        /// returns the computed type name on success.
        /// Since one or more types may required to be <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type, bool)">declared</see>,
        /// this may fail: this returns the (null on error) type name (instead of the "fluent" standard code writer).
        /// </summary>
        /// <param name="b">This code part.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="g">The generator.</param>
        /// <param name="type">The type whose name must be appended.</param>
        /// <param name="withUndefined">
        /// False to ignore the nullable informations on the <paramref name="type"/>.
        /// By default, if the type is nullable, the generated type name will be <c>|undefined</c>.
        /// </param>
        /// <param name="alwaysUsePocoClass">
        /// False to consider a IPoco interface as the interface: by default, all IPoco interface are mapped to
        /// their class type name (that is the primary interface name without the I prefix).
        /// </param>
        /// <returns>The generated type name (that can be reused in the same file) or null on error.</returns>
        public static string? AppendAndGetComplexTypeName( this ITSCodePart b,
                                                           IActivityMonitor monitor,
                                                           TypeScriptContext g,
                                                           NullableTypeTree type,
                                                           bool withUndefined = true,
                                                           bool alwaysUsePocoClass = true )
        {
            var p = b.CreatePart();
            return AppendComplexTypeName( p, monitor, g, type, withUndefined, alwaysUsePocoClass ) ? p.ToString() : null;
        }

        /// <summary>
        /// Appends a type that may be complex: a <see cref="TSTypeFile"/> may be declared for it and it may require
        /// multiple <see cref="TypeScriptFile.Imports"/>.
        /// <para>
        /// <c>typeof(void)</c> is mapped to <c>void</c>, <c>object</c> is mapped to <c>unknown</c>, "small" numerics are mapped to <c>number</c>,
        /// <c>long</c>, <c>ulong</c>, <c>decimal</c> and <c>BigInteger</c> are mapped to <c>BigInteger</c>, <c>bool</c> is mapped to <c>boolean</c>
        /// and <c>string</c> is mapped to <c>string</c>.
        /// Value tuple are mapped as array, list, set and dictionary are mapped to Array, Set or Map.
        /// </para>
        /// </summary>
        /// <param name="part">This code writer.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="g">The generator.</param>
        /// <param name="type">The type whose name must be appended.</param>
        /// <param name="withUndefined">
        /// False to ignore the nullable informations on the <paramref name="type"/>.
        /// By default, if the type is nullable, the generated type name will be <c>|undefined</c>.
        /// </param>
        /// <param name="alwaysUsePocoClass">
        /// False to consider a IPoco interface as the interface: by default, all IPoco interface are mapped to
        /// their class type name (that is the primary interface name without the I prefix).
        /// </param>
        /// <returns>True on success, false on error.</returns>
        public static bool AppendComplexTypeName( this ITSCodeWriter part,
                                                  IActivityMonitor monitor,
                                                  TypeScriptContext g,
                                                  NullableTypeTree type,
                                                  bool withUndefined = true,
                                                  bool alwaysUsePocoClass = true )
        {
            var t = type.Type;
            IPocoInterfaceInfo? iPoco;
            var luxonLib = new LibraryImport( "luxon", "^3.0.1", DependencyKind.PeerDependency );

            var intrinsicName = TypeScriptContext.IntrinsicTypeName( t );
            if( intrinsicName != null )
            {
                part.Append( intrinsicName );
            }
            else if( t == typeof(DateTime) )
            {
                part.File.Imports.EnsureImportFromLibrary( luxonLib, "DateTime" );
                part.Append( "DateTime" );
            }
            else if( t == typeof( DateTimeOffset ) )
            {
                part.File.Imports.EnsureImportFromLibrary( luxonLib, "DateTime" );
                part.Append( "DateTime" );
            }
            else if( t == typeof( TimeSpan ) )
            {
                part.File.Imports.EnsureImportFromLibrary( luxonLib, "Duration" );
                part.Append( "Duration" );
            }
            else if( t.IsArray )
            {
                part.Append( "Array<" );
                if( !AppendComplexTypeName( part, monitor, g, type.RawSubTypes[0] ) ) return false;
                part.Append( ">" );
            }
            else if( type.Kind.IsTupleType() )
            {
                part.Append( "[" );
                bool atLeastOne = false;
                foreach( var s in type.SubTypes )
                {
                    if( atLeastOne ) part.Append( ", " );
                    atLeastOne = true;
                    if( !AppendComplexTypeName( part, monitor, g, s ) ) return false;
                }
                part.Append( "]" );
            }
            else if( t.IsGenericType )
            {
                var tDef = t.GetGenericTypeDefinition();
                if( type.RawSubTypes.Count == 2 && (tDef == typeof( IDictionary<,> ) || tDef == typeof( Dictionary<,> )) )
                {
                    part.Append( "Map<" );
                    if( !AppendComplexTypeName( part, monitor, g, type.RawSubTypes[0] ) ) return false;
                    part.Append( "," );
                    if( !AppendComplexTypeName( part, monitor, g, type.RawSubTypes[1] ) ) return false;
                    part.Append( ">" );
                }
                else if( type.RawSubTypes.Count == 1 )
                {
                    if( tDef == typeof( ISet<> ) || tDef == typeof( HashSet<> ) )
                    {
                        part.Append( "Set<" );
                        if( !AppendComplexTypeName( part, monitor, g, type.RawSubTypes[0] ) ) return false;
                        part.Append( ">" );
                    }
                    else if( tDef == typeof( IList<> ) || tDef == typeof( List<> ) )
                    {
                        part.Append( "Array<" );
                        if( !AppendComplexTypeName( part, monitor, g, type.RawSubTypes[0] ) ) return false;
                        part.Append( ">" );
                    }
                }
                else
                {
                    if( !DeclareAndImportAndAppendTypeName( part, monitor, g, t ) ) return false;
                }
            }
            else if( alwaysUsePocoClass && (iPoco = g.PocoCodeGenerator.PocoSupport.Find( t )) != null )
            {
                var c = g.DeclareTSType( monitor, iPoco.Root.PocoClass, requiresFile: true );
                if( c == null ) return false;
                part.File.Imports.EnsureImport( c.File, c.TypeName );
                part.Append( c.TypeName );
            }
            else 
            {
                if( !DeclareAndImportAndAppendTypeName( part, monitor, g, t ) ) return false;
            }
            if( withUndefined && type.Kind.IsNullable() ) part.Append( "|undefined" );
            return true;
        }

        static bool DeclareAndImportAndAppendTypeName( ITSCodeWriter b, IActivityMonitor monitor, TypeScriptContext g, Type t )
        {
            var other = g.DeclareTSType( monitor, t, requiresFile: true );
            if( other == null ) return false;
            AppendImportedTypeName( b, other );
            return true;
        }


    }
}
