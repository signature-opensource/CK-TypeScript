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
    /// Xml comment is a block comment that starts with "&lt;!--" and ends with "--&gt;".
    /// By default, as per https://html.spec.whatwg.org/multipage/parsing.html#parse-errors incorrectly-closed-comment,
    /// we also allow that the end with "--!&gt;".
    /// </summary>
    /// <param name="c">This head.</param>
    /// <param name="strict">True to not accept "--!&gt;" comment end.</param>
    public static void AcceptXmlComment( this ref TriviaHead c, bool strict = false )
    {
        if( c.Head.StartsWith( "<!--" ) )
        {
            int i = c.Head.IndexOf( "-->" );
            if( i >= 0 )
            {
                c.Accept( TokenTypeExtensions.GetTriviaBlockCommentType( 4, 3 ), i + 3 );
                return;
            }
            if( !strict )
            {
                i = c.Head.IndexOf( "--!>" );
                if( i > 0 )
                {
                    c.Accept( TokenTypeExtensions.GetTriviaBlockCommentType( 4, 4 ), i + 3 );
                    return;
                }
            }
            c.EndOfInput( TokenTypeExtensions.GetTriviaBlockCommentType( 4, 3 ) );
        }
    }

    /// <summary>
    /// Xml CDATA is a block comment that starts with "&lt;![CDATA[" and ends with "]]&gt;".
    /// <para>
    /// As per https://html.spec.whatwg.org/multipage/parsing.html#parse-errors cdata-in-html-content, this
    /// is not valid in html (but can appear in foreign elements like &lt;math&gt; (MathML), so we handle
    /// it everywhere.
    /// </para>
    /// </summary>
    /// <param name="c">This head.</param>
    public static void AcceptXmlCDATA( this ref TriviaHead c )
    {
        if( c.Head.StartsWith( "<![CDATA[" ) )
        {
            int i = c.Head.IndexOf( "]]>" );
            if( i < 0 )
            {
                c.EndOfInput( TokenTypeExtensions.GetTriviaBlockCommentType( 9, 3 ) );
            }
            else
            {
                c.Accept( TokenTypeExtensions.GetTriviaBlockCommentType( 9, 3 ), i + 3 );
            }
        }
    }
}
