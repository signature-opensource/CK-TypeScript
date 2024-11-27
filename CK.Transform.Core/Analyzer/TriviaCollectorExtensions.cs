using System;

namespace CK.Transform.Core;

/// <summary>
/// Standard micro parsers of trivias.
/// </summary>
public static class TriviaCollectorExtensions
{
    /// <summary>
    /// A <see cref="TokenType.LineComment"/> is a C-like language comment that starts with a "//" and ends with a new line.
    /// <para>
    /// This kind of trivia cannot be on error: if the end of input is reached, the comment is valid.
    /// </para>
    /// </summary>
    /// <param name="c">This collector.</param>
    /// <returns>The successfully parsed length or 0 when <see cref="TriviaCollector.Head"/> doesn't start with "//".</returns>
    public static int LineComment( this ref TriviaCollector c )
    {
        if( c.Head.StartsWith( "//" ) )
        {
            int iS = 1;
            while( ++iS < c.Head.Length && c.Head[iS] != '\n' ) ;
            return c.Accept( TokenType.SqlComment, iS );
        }
        return 0;
    }

    /// <summary>
    /// Same as <see cref="LineComment(ref TriviaCollector)"/> but with a starting "--".
    /// </summary>
    /// <param name="c">This collector.</param>
    /// <returns>The successfully parsed length or 0 when <see cref="TriviaCollector.Head"/> doesn't start with "--".</returns>
    public static int SqlComment( this ref TriviaCollector c )
    {
        if( c.Head.StartsWith( "--" ) )
        {
            int iS = 1;
            while( ++iS < c.Head.Length && c.Head[iS] != '\n' ) ;
            return c.Accept( TokenType.SqlComment, iS );
        }
        return 0;
    }

    /// <summary>
    /// A <see cref="TokenType.StarComment"/> is a C-like language block comment that starts with a "/*" and ends "*/".
    /// <para>
    /// This kind of trivia can be on error when the end of input is reached before the terminator.
    /// </para>
    /// </summary>
    /// <param name="c">This collector.</param>
    /// <returns>
    /// The successfully parsed length, 0 when <see cref="TriviaCollector.Head"/> doesn't start with "/*" or
    /// a negative value one error (see <see cref="TriviaCollector"/>).
    /// </returns>
    public static int StartComment( this ref TriviaCollector c )
    {
        if( c.Head.StartsWith( "/*" ) )
        {
            int iS = 1;
            for(; ; )
            {
                if( ++iS == c.Head.Length ) return c.Error( TokenType.StarComment );
                if( c.Head.StartsWith( "/*" ) )
                {
                    return c.Accept( TokenType.StarComment, iS + 2 );
                }
            }
        }
        return 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static int XmlComment( this ref TriviaCollector c )
    {
        if( c.Head.StartsWith( "<!--" ) )
        {
            int iS = 3;
            for(; ; )
            {
                if( ++iS == c.Head.Length ) return c.Error( TokenType.XmlComment );
                if( c.Head.StartsWith( "-->" ) )
                {
                    iS += 3;
                    return c.Accept( TokenType.XmlComment, iS );
                }
            }
        }
        return 0;
    }

}
