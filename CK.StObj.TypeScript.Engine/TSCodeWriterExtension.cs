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
        static public T AppendEnumDefinition<T>( this T @this, IActivityMonitor monitor, Type enumType, string typeName, bool export ) where T : ITSCodePart
        {
            if( !enumType.IsEnum ) throw new ArgumentException( $"Must be an enum: {enumType.Name}.", nameof( enumType ) );
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
        /// <param name="t">The type name to append.</param>
        /// <returns>The code writer.</returns>
        public static T AppendImportedTypeName<T>( this T @this, TSTypeFile t ) where T : ITSCodePart
        {
            @this.File.Imports.EnsureImport( t.TypeName, t.File );
            @this.Append( t.TypeName );
            return @this;
        }

        /// <summary>
        /// Appends a type that may be complex: a <see cref="TSTypeFile"/> may be declared for it and it may require
        /// multiple <see cref="TypeScriptFile.Imports"/>.
        /// Since types may be <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type, bool)"/>, this may
        /// fail, so this returns a boolean (instead of the "fluent" standard code writer).
        /// <para>
        /// <c>typeof(void)</c> is mapped to <c>void</c>, <c>object</c> is mapped to <c>unknown</c>, <c>int</c>, <c>float</c>
        /// and <c>double</c> are mapped to <c>number</c>, <c>bool</c> is mapped to <c>boolean</c> and <c>string</c> is mapped to <c>string</c>.
        /// Value tuple are mapped as array, list, set and dictionary are mapped to Array, Set or Map.
        /// </para>
        /// </summary>
        /// <param name="b">This code writer.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="g">The generator.</param>
        /// <param name="t">The type whose name must be appended.</param>
        /// <returns>True on success, false on error.</returns>
        public static bool AppendComplexTypeName( this ITSCodePart b, IActivityMonitor monitor, TypeScriptContext g, Type t )
        {
            bool success = true;
            if( t.IsArray )
            {
                b.Append( "Array<" );
                success &= AppendComplexTypeName( b, monitor, g, t.GetElementType()! );
                b.Append( ">" );
            }
            else if( t.IsValueTuple() )
            {
                b.Append( "[" );
                foreach( var s in t.GetGenericArguments() )
                {
                    success &= AppendComplexTypeName( b, monitor, g, s );
                }
                b.Append( "]" );
            }
            else if( t.IsGenericType )
            {
                var tDef = t.GetGenericTypeDefinition();
                if( tDef == typeof( IDictionary<,> ) || tDef == typeof( Dictionary<,> ) )
                {
                    var args = t.GetGenericArguments();
                    b.Append( "Map<" );
                    success &= AppendComplexTypeName( b, monitor, g, args[0] );
                    b.Append( "," );
                    success &= AppendComplexTypeName( b, monitor, g, args[1] );
                    b.Append( ">" );
                }
                else if( tDef == typeof( ISet<> ) || tDef == typeof( HashSet<> ) )
                {
                    b.Append( "Set<" );
                    success &= AppendComplexTypeName( b, monitor, g, t.GetGenericArguments()[0] );
                    b.Append( ">" );
                }
                else if( tDef == typeof( IList<> ) || tDef == typeof( List<> ) )
                {
                    b.Append( "Array<" );
                    success &= AppendComplexTypeName( b, monitor, g, t.GetGenericArguments()[0] );
                    b.Append( ">" );
                }
                else
                {
                    success &= DeclareAndImportAndAppendTypeName( b, monitor, g, t );
                }
            }
            else if( t == typeof( void ) ) b.Append( "void" );
            else if( t == typeof( int ) || t == typeof( float ) || t == typeof( double ) ) b.Append( "number" );
            else if( t == typeof( bool ) ) b.Append( "boolean" );
            else if( t == typeof( string ) ) b.Append( "string" );
            else if( t == typeof( object ) ) b.Append( "unknown" );
            else
            {
                success &= DeclareAndImportAndAppendTypeName( b, monitor, g, t );
            }
            return success;
        }

        /// <summary>
        /// Appends a type that may be complex: a <see cref="TSTypeFile"/> may be declared for it and it may require
        /// multiple <see cref="TypeScriptFile.Imports"/>.
        /// Since one or more types may required to be <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type, bool)">declared</see>,
        /// this may fail, so this returns a boolean (instead of the "fluent" standard code writer).
        /// <para>
        /// <c>typeof(void)</c> is mapped to <c>void</c>, <c>object</c> is mapped to <c>unknown</c>, <c>int</c>, <c>float</c>
        /// and <c>double</c> are mapped to <c>number</c>, <c>bool</c> is mapped to <c>boolean</c> and <c>string</c> is mapped to <c>string</c>.
        /// Value tuple are mapped as array, list, set and dictionary are mapped to Array, Set or Map.
        /// </para>
        /// </summary>
        /// <param name="b">This code writer.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="g">The generator.</param>
        /// <param name="t">The type whose name must be appended.</param>
        /// <returns>True on success, false on error.</returns>
        public static bool AppendComplexTypeName( this ITSCodePart b, IActivityMonitor monitor, TypeScriptContext g, NullableTypeTree type, bool withUndefined = true )
        {
            bool success = true;
            var t = type.Type;
            if( t.IsArray )
            {
                b.Append( "Array<" );
                success &= AppendComplexTypeName( b, monitor, g, type.SubTypes[0] );
                b.Append( ">" );
            }
            else if( type.Kind.IsTupleType() )
            {
                b.Append( "[" );
                bool atLeastOne = false;
                foreach( var s in type.SubTypes )
                {
                    if( atLeastOne ) b.Append( ", " );
                    atLeastOne = true;
                    success &= AppendComplexTypeName( b, monitor, g, s );
                }
                b.Append( "]" );
            }
            else if( t.IsGenericType )
            {
                var tDef = t.GetGenericTypeDefinition();
                if( type.SubTypes.Count == 2 && (tDef == typeof( IDictionary<,> ) || tDef == typeof( Dictionary<,> )) )
                {
                    b.Append( "Map<" );
                    success &= AppendComplexTypeName( b, monitor, g, type.SubTypes[0] );
                    b.Append( "," );
                    success &= AppendComplexTypeName( b, monitor, g, type.SubTypes[1] );
                    b.Append( ">" );
                }
                else if( type.SubTypes.Count == 1 )
                {
                    if( tDef == typeof( ISet<> ) || tDef == typeof( HashSet<> ) )
                    {
                        b.Append( "Set<" );
                        success &= AppendComplexTypeName( b, monitor, g, type.SubTypes[0] );
                        b.Append( ">" );
                    }
                    else if( tDef == typeof( IList<> ) || tDef == typeof( List<> ) )
                    {
                        b.Append( "Array<" );
                        success &= AppendComplexTypeName( b, monitor, g, type.SubTypes[0] );
                        b.Append( ">" );
                    }
                }
                else
                {
                    success &= DeclareAndImportAndAppendTypeName( b, monitor, g, t );
                }
            }
            else if( t == typeof( void ) ) b.Append( "void" );
            else if( t == typeof( int ) || t == typeof( float ) || t == typeof( double ) ) b.Append( "number" );
            else if( t == typeof( bool ) ) b.Append( "boolean" );
            else if( t == typeof( string ) ) b.Append( "string" );
            else if( t == typeof( object ) ) b.Append( "unknown" );
            else
            {
                success &= DeclareAndImportAndAppendTypeName( b, monitor, g, t );
            }
            if( withUndefined && type.Kind.IsNullable() ) b.Append( "|undefined" );
            return success;
        }

        static bool DeclareAndImportAndAppendTypeName( ITSCodePart b, IActivityMonitor monitor, TypeScriptContext g, Type t )
        {
            var other = g.DeclareTSType( monitor, t, requiresFile: true );
            if( other == null ) return false;
            AppendImportedTypeName( b, other );
            return true;
        }


    }
}
