using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Diagnostics;
using System.Xml.Linq;
using CK.Core;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Provides Append fluent extension methods to <see cref="ITSCodeWriter"/> specializations.
    /// </summary>
    public static class TSCodeWriterExtensions
    {
        /// <summary>
        /// Appends raw C# code only once: the code itself is used as a key in <see cref="ITSCodePart.Memory"/> to
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
        /// Appends raw C# code.
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
        /// Appends either "true" or "false".
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="b">The boolean value.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T Append<T>( this T @this, bool b ) where T : ITSCodeWriter => @this.Append( b ? "true" : "false" );

        /// <summary>
        /// Appends the source representation of an integer value.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="i">The integer.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T Append<T>( this T @this, int i ) where T : ITSCodeWriter
        {
            return @this.Append( i.ToString( CultureInfo.InvariantCulture ) );
        }

        /// <summary>
        /// Appends the source representation of a <see cref="Double"/> value.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="d">The double.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T Append<T>( this T @this, double d ) where T : ITSCodeWriter
        {
            return @this.Append( d.ToString( CultureInfo.InvariantCulture ) );
        }

        /// <summary>
        /// Appends the source representation of a <see cref="Single"/> value.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="f">The float.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T Append<T>( this T @this, float f ) where T : ITSCodeWriter
        {
            return @this.Append( f.ToString( CultureInfo.InvariantCulture ) ).Append( "f" );
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
                @this.Append( "'" );
                int last = 0, count = 0;
                foreach( var c in s )
                {
                    var enc = Encoded( c );
                    if( enc != null )
                    {
                        if( count > 0 ) @this.Append( s.Substring( last, count ) );
                        @this.Append( enc );
                        count = -1;
                    }
                    ++count;
                }
                if( count > 0 ) @this.Append( s.Substring( last, count ) );
                @this.Append( "'" );
            }
            return @this;

            static string? Encoded( char c )
            {
                switch( c )
                {
                    case '\\': return @"\\";
                    case '\'': return @"\'";
                    case '"': return @"\""";
                    case '\r': return @"\r";
                    case '\n': return @"\n'";
                    case '\t': return @"\t";
                    case '\0': return @"\0";
                    case '\b': return @"\b";
                    case '\v': return @"\x0B";
                    case '\f': return @"\f";
                }
                int vC = c;
                if( vC < 32
                    || (vC >= 127 && vC <= 160)
                    || vC >= 888 )
                {
                    return "\\u" + vC.ToString( "X4" );
                }
                return null;
            }

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
        /// basic types are supported.
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
        static public T AppendIdentifier<T>( this T @this, string name ) where T : ITSCodeWriter => Append( @this, @this.File.Folder.Root.ToIdentifier( name ) );

        /// <summary>
        /// Appends the code source for an untyped object.
        /// Only types that are implemented through one of the existing Append, AppendArray (all IEnumerable are
        /// handled) and enum values.
        /// extension methods are supported: an <see cref="ArgumentException"/> is thrown for unsupported type.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="o">The object. Can be null.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T Append<T>( this T @this, object? o ) where T : ITSCodeWriter
        {
            if( o == DBNull.Value ) return @this.Append( "null" );
            switch( o )
            {
                case null: return @this.Append( "null" );
                case bool x: return Append( @this, x );
                case int x: return Append( @this, x );
                case char x: return AppendSourceChar( @this, x );
                case double x: return Append( @this, x );
                case float x: return Append( @this, x );
                case IEnumerable x: return AppendArray( @this, x );
            }
            throw new ArgumentException( "Unknown type: " + o.GetType().AssemblyQualifiedName );
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
                 .Append( decl.Optional ? "?: " : ": " )
                 .Append( decl.Type );
            if( !String.IsNullOrWhiteSpace( decl.DefaultValue ) )
            {
                @this.Append( " = " ).Append( decl.DefaultValue );
            }
            return @this;
        }



    }
}
