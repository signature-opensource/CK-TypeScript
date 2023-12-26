using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Diagnostics;
using System.Xml.Linq;
using CK.Core;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Provides Append fluent extension methods to <see cref="ITSCodeWriter"/> specializations.
    /// </summary>
    public static class TSCodeWriterExtensions
    {
        /// <summary>
        /// Combines this import with another one.
        /// </summary>
        /// <param name="this">This import.</param>
        /// <param name="other">The other to combine with this.</param>
        /// <returns>A combined import.</returns>
        public static Action<ITSFileImportSection>? Combine( this Action<ITSFileImportSection>? @this, Action<ITSFileImportSection>? other )
        {
            if( @this == null ) return other;
            if( other == null ) return @this;
            return import => { @this.Invoke( import ); other.Invoke( import ); };
        }

        /// <summary>
        /// Appends raw TypeScript code only once: the code itself is used as a key in <see cref="ITSCodePart.Memory"/> to
        /// avoid adding it twice.
        /// </summary>
        /// <typeparam name="T">Must be a <see cref="ITSCodePart"/>.</typeparam>
        /// <param name="this">This code part.</param>
        /// <param name="code">Raw code to append. Must not be null, empty or white space.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T AppendOnce<T>( this T @this, string code ) where T : ITSCodePart
        {
            if( String.IsNullOrWhiteSpace( code ) ) throw new ArgumentException( "To guaranty AppendOnce semantics, code must not be null or white space.", nameof( code ) );
            if( !@this.Memory.ContainsKey( code ) )
            {
                @this.Append( code );
                @this.Memory.Add( code, null );
            }
            return @this;
        }

        /// <summary>
        /// Appends raw TypeScript code.
        /// This is the most basic Append method to use.
        /// Use <see cref="AppendSourceString{T}(T, string)"/> to append the source string representation.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="code">Raw code to append.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T Append<T>( this T @this, string? code ) where T : ITSCodeWriter
        {
            @this.DoAdd( code );
            return @this;
        }

        /// <summary>
        /// Appends a raw character.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="c">Character to append.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T Append<T>( this T @this, char c ) where T : ITSCodeWriter
        {
            @this.DoAdd( c.ToString() );
            return @this;
        }

        /// <summary>
        /// Appends a white space.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T Space<T>( this T @this ) where T : ITSCodeWriter => @this.Append( " " );

        /// <summary>
        /// Appends a <see cref="Environment.NewLine"/>.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T NewLine<T>( this T @this ) where T : ITSCodeWriter => @this.Append( Environment.NewLine );

        /// <summary>
        /// Appends a "{" on a new independent line.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T OpenBlock<T>( this T @this ) where T : ITSCodeWriter => @this.Append( " {" ).NewLine();

        /// <summary>
        /// Appends a "}" on a new independent line.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="withSemiColon">True to add a ";" after the bracket.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T CloseBlock<T>( this T @this, bool withSemiColon = false ) where T : ITSCodeWriter => @this.NewLine().Append( withSemiColon ? "};" : "}" ).NewLine();

        /// <summary>
        /// Appends a <see cref="ITSType.TypeName"/> with its <see cref="ITSType.RequiredImports"/>.
        /// </summary>
        /// <typeparam name="T">The code writer type.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="typeName">The <see cref="ITSType.TypeName"/> to append and import.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        public static T AppendTypeName<T>( this T @this, ITSType typeName ) where T : ITSCodeWriter
        {
            typeName.EnsureRequiredImports( @this.File.Imports );
            @this.Append( typeName.TypeName );
            return @this;
        }

        /// <summary>
        /// Appends the source representation of a character: "'c'".
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="c">The character.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T AppendSourceChar<T>( this T @this, char c ) where T : ITSCodeWriter
        {
            switch( c )
            {
                case '\\': return @this.Append( @"'\\'" );
                case '\r': return @this.Append( @"'\r'" );
                case '\n': return @this.Append( @"'\n'" );
                case '\t': return @this.Append( @"'\t'" );
                case '\0': return @this.Append( @"'\0'" );
                case '\b': return @this.Append( @"'\b'" );
                case '\v': return @this.Append( @"'\v'" );
                case '\a': return @this.Append( @"'\a'" );
                case '\f': return @this.Append( @"'\f'" );
            }
            int vC = c;
            if( vC < 32
                || (vC >= 127 && vC <= 160)
                || vC >= 888 )
            {
                return @this.Append( "'\\u" ).Append( vC.ToString( "X4" ) ).Append( "'" );
            }
            return @this.Append( "'" ).Append( c.ToString() ).Append( "'" );
        }

        /// <summary>
        /// Appends the source representation of the string.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="s">The string. Can be null.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T AppendSourceString<T>( this T @this, string? s ) where T : ITSCodeWriter
        {
            if( s == null ) @this.Append( "null" );
            else
            {
                Throw.DebugAssert( System.Web.HttpUtility.JavaScriptStringEncode( s, true ) == JsonSerializer.Serialize( s ) );
                @this.Append( System.Web.HttpUtility.JavaScriptStringEncode( s, true ) );
            }
            return @this;
        }

        /// <summary>
        /// Appends multiple string (raw C# code) at once, separated with a comma by default.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="strings">The string. Can be null or empty.</param>
        /// <param name="separator">Separator between the strings.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T Append<T>( this T @this, IEnumerable<string> strings, string? separator = ", " ) where T : ITSCodeWriter
        {
            if( strings != null )
            {
                if( String.IsNullOrEmpty( separator ) ) separator = null;
                bool already = false;
                foreach( var s in strings )
                {
                    if( already )
                    {
                        if( separator != null ) @this.Append( separator );
                    }
                    else already = true;
                    Append( @this, s );
                }
            }
            return @this;
        }

        /// <summary>
        /// Appends the code of a collection of objects of a given type <typeparamref name="T"/>.
        /// The code is either "null", "[]" or an array
        /// with the items appended with <see cref="Append{T}(T, object)"/>: only
        /// types that are mapped to an associated <see cref="ITSType"/> are supported.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="e">Set of items for which code must be generated. Can be null.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T AppendArray<T>( this T @this, IEnumerable? e ) where T : ITSCodeWriter
        {
            if( e == null ) return @this.Append( "null" );
            @this.Append( "[" );
            bool already = false;
            foreach( object? x in e )
            {
                if( already ) @this.Append( "," );
                else already = true;
                Append( @this, x );
            }
            return @this.Append( "]" );
        }

        /// <summary>
        /// Appends an identifier (simple call to <see cref="TypeScriptRoot.ToIdentifier(string)"/>).
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="name">.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T AppendIdentifier<T>( this T @this, string name ) where T : ITSCodeWriter => Append( @this, @this.File.Root.ToIdentifier( name ) );

        /// <summary>
        /// Calls <see cref="TryAppend{T}(T, object?)"/> and throws an <see cref="ArgumentException"/> if
        /// the object's type is not handled.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="o">The object. Can be null.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T Append<T>( this T @this, object? o ) where T : ITSCodeWriter
        {
            if( !TryAppend( @this, o ) )
            {
                Debug.Assert( o != null );
                var t = o.GetType();
                var tsType = @this.File.Root.TSTypes.Find( t );
                if( tsType == null )
                {
                    Throw.ArgumentException( $"Unable to write a value of type '{t.ToCSharpName()}'. It is not mapped to any registered TSType." );
                }
                Throw.ArgumentException( $"Unable to write a value of type '{t.ToCSharpName()}'. It is mapped to the TSType '{tsType.TypeName}' that is not able to write this type." );
            }
            return @this;
        }

        /// <summary>
        /// Tries to appends the code source for an untyped object.
        /// Only types that have an associated <see cref="ITSType"/> are handled.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="o">The object. Can be null.</param>
        /// <returns>True on success, false on error.</returns>
        static public bool TryAppend<T>( this T @this, object? o ) where T : ITSCodeWriter
        {
            if( o == DBNull.Value || o == null ) @this.Append( "null" );
            else
            {
                var t = o.GetType();
                var tsType = @this.File.Root.TSTypes.Find( t );
                if( tsType == null || !tsType.TryWriteValue( @this, o ) )
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Creates a segment of code inside this part.
        /// This signature allows a fluent code to "emit" one or more insertion points.
        /// </summary>
        /// <typeparam name="T">The code part type.</typeparam>
        /// <param name="this">This code part.</param>
        /// <param name="part">The part to use to inject code at this location (or at the top).</param>
        /// <param name="closer">Optional closer of the subordinate part.</param>
        /// <param name="top">Optionally creates the new part at the start of the code instead of at the current writing position in the code.</param>
        /// <returns>This part to enable fluent syntax.</returns>
        public static T CreatePart<T>( this T @this, out ITSCodePart part, string closer = "", bool top = false ) where T : ITSCodePart
        {
            part = @this.CreatePart( closer, top );
            return @this;
        }

        /// <summary>
        /// Creates a keyed part of code inside this part.
        /// This signature allows a fluent code to "emit" one or more insertion points.
        /// </summary>
        /// <typeparam name="T">The code part type.</typeparam>
        /// <param name="this">This code part.</param>
        /// <param name="part">Outputs the keyed part to use to inject code at this location (or at the <paramref name="top"/>).</param>
        /// <param name="key">The <see cref="ITSKeyedCodePart.Key"/>.</param>
        /// <param name="closer">Optional closer of the subordinate part.</param>
        /// <param name="top">Optionally creates the new part at the start of the code instead of at the current writing position in the code.</param>
        /// <returns>This code part to enable fluent syntax.</returns>
        public static T CreateKeyedPart<T>( this T @this, out ITSKeyedCodePart part, object key, string closer = "", bool top = false ) where T : ITSCodePart
        {
            part = @this.CreateKeyedPart( key, closer, top );
            return @this;
        }

        /// <summary>
        /// Fluent function application: this enables a procedural fragment to be inlined in a fluent code.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="f">Fluent function to apply.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        public static T Append<T>( this T @this, Func<T, T> f ) where T : ITSCodeWriter => f( @this );

        /// <summary>
        /// Fluent action application: this enables a procedural fragment to be inlined in a fluent code.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="f">Action to apply to this code writer.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        public static T Append<T>( this T @this, Action<T> f ) where T : ITSCodeWriter
        {
            f( @this );
            return @this;
        }

        /// <summary>
        /// Appends a <see cref="TypeScriptVarType"/>.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="decl">The declaration.</param>
        /// <param name="prefixName">Optional prefix to inject before the name.</param>
        /// <param name="withComment">False to skip the initial <see cref="TypeScriptVarType.Comment"/> if any.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        public static T Append<T>( this T @this, TypeScriptVarType decl, string? prefixName = null, bool withComment = true ) where T : ITSCodeWriter
        {
            if( withComment && !String.IsNullOrWhiteSpace( decl.Comment ) ) @this.AppendDocumentation( decl.Comment );
            @this.Append( prefixName )
                 .Append( decl.Name )
                 .Append( decl.TSType.IsNullable ? "?: " : ": " )
                 .AppendTypeName( decl.TSType );
            if( !String.IsNullOrWhiteSpace( decl.DefaultValueSource ) )
            {
                @this.Append( " = " ).Append( decl.DefaultValueSource );
            }
            return @this;
        }

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
            if( uT == typeof( UInt32 ) || uT == typeof( Int64 ) || uT == typeof( UInt64 ) )
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


    }
}
