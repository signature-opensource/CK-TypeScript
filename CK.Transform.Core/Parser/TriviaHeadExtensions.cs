using System;

namespace CK.Transform.Core;

/// <summary>
/// Standard micro parsers of trivias.
/// These extension methods are <see cref="TriviaParser"/> and can easily be combined
/// into composite <see cref="TriviaParser"/>.
/// </summary>
public static class TriviaHeadExtensions
{
    /// <summary>
    /// A <see cref="NodeType.LineComment"/> is a C-like language comment that starts with a "//" and ends with a new line.
    /// <para>
    /// This kind of trivia cannot be on error: if the end of input is reached, the comment is valid.
    /// </para>
    /// </summary>
    /// <param name="c">This head.</param>
    public static void AcceptLineComment( this ref TriviaHead c )
    {
        if( c.Head.StartsWith( "//" ) )
        {
            int iS = 1;
            while( ++iS < c.Head.Length && c.Head[iS] != '\n' ) ;
            c.Accept( NodeType.SqlComment, iS );
        }
    }

    /// <summary>
    /// Same as <see cref="LineComment(ref TriviaCollector)"/> but with a starting "--".
    /// </summary>
    /// <param name="c">This head.</param>
    public static void AcceptSqlComment( this ref TriviaHead c )
    {
        if( c.Head.StartsWith( "--" ) )
        {
            int iS = 1;
            while( ++iS < c.Head.Length && c.Head[iS] != '\n' ) ;
            c.Accept( NodeType.SqlComment, iS );
        }
    }

    /// <summary>
    /// A <see cref="NodeType.StarComment"/> is a C-like language block comment that starts with a "/*" and ends "*/".
    /// <para>
    /// This kind of trivia can be on error when the end of input is reached before the terminator.
    /// </para>
    /// </summary>
    /// <param name="c">This head.</param>
    public static void AcceptStartComment( this ref TriviaHead c )
    {
        if( c.Head.StartsWith( "/*" ) )
        {
            int i = c.Head.IndexOf( "*/" );
            if( i < 0 )
            {
                c.EndOfInput( NodeType.StarComment );
            }
            else
            {
                c.Accept( NodeType.StarComment, i + 2 );
            }
        }
    }

    /// <summary>
    /// A <see cref="NodeType.StarComment"/> that is a C-like language block comment that starts with a "/*" and ends "*/"
    /// and can contain nested  "/* ... */" comments.
    /// <para>
    /// This kind of trivia can be on error when the end of input is reached before the terminator.
    /// </para>
    /// </summary>
    /// <param name="c">This head.</param>
    public static void AcceptRecursiveStartComment( this ref TriviaHead c )
    {
        var h = c.Head;
        if( h.StartsWith( "/*" ) )
        {
            h = h.Slice( 2 );
            int depth = 1;
            for( ; ; )
            {
                int iE = h.IndexOf( "*/" );
                if( iE < 0 )
                {
                    c.EndOfInput( NodeType.StarComment );
                    return;
                }
                depth += h.Slice( 0, iE ).Count( "/*" ) - 1;
                h = h.Slice( iE + 2 );
                if( depth == 0 ) break;
            }
            c.Head.Overlaps( h, out var pos );
            c.Accept( NodeType.StarComment, pos );
        }
    }

    /// <summary>
    /// Xml comment is a block comment that starts with "<!--" and ends with "-->".
    /// </summary>
    /// <param name="c">This head.</param>
    public static void AcceptXmlComment( this ref TriviaHead c )
    {
        if( c.Head.StartsWith( "<!--" ) )
        {
            int i = c.Head.IndexOf( "-->" );
            if( i < 0 )
            {
                c.EndOfInput( NodeType.XmlComment );
            }
            else
            {
                c.Accept( NodeType.XmlComment, i + 3 );
            }
        }
    }
}
