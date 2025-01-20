using CK.Core;
using CK.Transform.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CK.Html.Transform;



public sealed partial class HtmlAnalyzer : Tokenizer, IAnalyzer
{
    bool _inAttribute;

    public string LanguageName => HtmlLanguage._languageName;

    public HtmlAnalyzer()
    {
    }

    /// <summary>
    /// When in attributes, this is true: <see cref="HtmlTokenType.AttributeName"/>, <see cref="TokenType.Equals"/> and <see cref="HtmlTokenType.AttributeValue"/>
    /// will have regular trivias whith whitespaces.
    /// <para>
    /// This is false outside attributes: <see cref="HtmlTokenType.Text"/> contains whitespaces.
    /// </para>
    /// <para>
    /// Note that we skip &lt;!-- comment --&gt; (and CDATA) to appear in attributes as they are invalid there: in attributes,
    /// only whitespaces trivias will appear.
    /// </para>
    /// </summary>
    public bool HandleWhiteSpaceTrivias => _inAttribute;

    protected override void Reset( ReadOnlyMemory<char> text )
    {
        _inAttribute = false;
        base.Reset( text );
    }

    protected override void ParseTrivia( ref TriviaHead c )
    {
        if( !_inAttribute )
        {
            c.AcceptXmlComment();
            c.AcceptXmlCDATA();
        }
    }

    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        int iS = 0;

        if( _inAttribute )
        {
            return LowLevelTokenizeAttribute( head );
        }

        text:
        while( iS < head.Length && head[iS] != '<' ) iS++;
        Throw.DebugAssert( iS == head.Length || head[iS] == '<' );
        if( iS >= head.Length - 1 ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, head.Length );
        // A tag must start with [A-Za-z].
        // If the next char is not a ascii letter or the ending slash, continue as text.
        var c = head[++iS];
        bool isEnding = c == '/';
        if( !isEnding )
        {
            if( !char.IsAsciiLetter( c ) ) goto text;
            // If there is text before the <[A-Za-z], emit it. The tag will be processed next time.
            if( iS > 2 ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS - 1 );
        }
        else
        {
            if( iS >= head.Length - 1 ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, head.Length );
            c = head[++iS];
            if( !char.IsAsciiLetter( c ) ) goto text;
            // If there is text before the </[A-Za-z], emit it. The tag will be processed next time.
            if( iS > 3 ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS - 1 );
        }
        // In a tag name: <[a-zA-Z] or a closing </[a-zA-Z].
        bool isWhitespace = false;
        while( !(isWhitespace = char.IsWhiteSpace( c )) && c != '/' && c != '>' && c != '<' )
        {
            // End of input: emit the "<tag" or "</tag" as text.
            if( ++iS == head.Length ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );
            c = head[iS];
        }
        // Eats all the white spaces after the tag name.
        int endTagName = iS;
        if( isWhitespace )
        {
            do
            {
                // End of input: emit the "<tag" (or "</tag     ") as text.
                if( ++iS == head.Length ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );
                c = head[iS];
            }
            while( char.IsWhiteSpace( c ) && c != '/' && c != '>' && c != '<' );
        }
        if( isEnding )
        {
            for( ; ; )
            {
                if( c == '>' )
                {
                    return new LowLevelToken( (TokenType)HtmlTokenType.EndingTag, iS + 1 );
                }
                if( c == '<' || ++iS == head.Length ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );
                c = head[iS];
            }
        }
        if( c == '>' ) 
        {
            // StartingEmptyElement or EmptyVoidElement (no attributes).
            var t = VoidElements().IsMatch( head.Slice( 1, endTagName - 1 ) )
                        ? HtmlTokenType.EmptyVoidElement
                        : HtmlTokenType.StartingEmptyElement;
            return new LowLevelToken( (TokenType)t, iS + 1 );
        }
        if( c == '/' ) // Should now be '>'... but who knows...
        {
            // End of input: emit the "<tag /" as text.
            if( ++iS == head.Length ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );
            // Invalid "<tag /x" where 'x' is not '>': consider it text.
            if( head[iS] != '>' ) goto text;
            return new LowLevelToken( (TokenType)HtmlTokenType.EmptyElement, iS + 1 );
        }
        if( c == '<' )
        {
            // "<Ouch <": emits "<Ouch " as text.
            return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );
        }
        // We are on the start of an attribute name. The next token (GenericIdentifier) will have the whitespace as a Trivia,
        // and the following tokens use regular whitespace trivia handling.
        _inAttribute = true;
        var tVoid = VoidElements().IsMatch( head.Slice( 1, endTagName - 1 ) )
            ? HtmlTokenType.StartingVoidElement
            : HtmlTokenType.StartingTag;
        return new LowLevelToken( (TokenType)tVoid, endTagName );
    }

    LowLevelToken LowLevelTokenizeAttribute( ReadOnlySpan<char> head )
    {
        var c = head[0];
        if( c == '<' )
        {
            // Here we give up: error.
            return new LowLevelToken( (TokenType)HtmlTokenType.StartingTag | TokenType.ErrorClassBit, 1 );
        }
        if( c == '/' )
        {
            if( head.Length == 1 || head[1] != '>' )
            {
                // Here we give up: error.
                return new LowLevelToken( (TokenType)HtmlTokenType.EndTokenTag | TokenType.ErrorClassBit, 1 );
            }
            _inAttribute = false;
            return new LowLevelToken( (TokenType)HtmlTokenType.EndTokenTag, 2 );
        }
        if( c == '>' )
        {
            _inAttribute = false;
            return new LowLevelToken( TokenType.GreaterThan, 1 );
        }
        if( c == '=' ) return new LowLevelToken( TokenType.Equals, 1 );
        if( c == '\'' || c == '"' ) return LowLevelToken.BasicallyReadQuotedString( head );
        int iS = 0;
        while( !char.IsWhiteSpace( c )
               && c != '/' && c != '>' && c != '=' && c != '<' && c != '\'' && c != '"' )
        {
            // End of input: emit the identifier.
            if( ++iS == head.Length ) break;
            c = head[iS];
        }
        return new LowLevelToken( TokenType.GenericIdentifier, iS );
    }

    public AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        Reset( text );
        return Parse();
    }


    protected override TokenError? Tokenize( ref TokenizerHead head )
    {
        for( ; ; )
        {
            // Consider low level token errors as hard errors.
            // No need to be error tolerant here.
            var type = head.LowLevelTokenType;
            if( type.IsError() )
            {
                return type is TokenType.EndOfInput or TokenType.None
                        ? null
                        : head.CreateHardError( "Invalid markup.", type );
            }
            head.AcceptLowLevelToken();
        }
    }

   
    [GeneratedRegex( "^(area|base|br|col|embed|hr|img|input|link|meta|source|track|wbr)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture )]
    private static partial Regex VoidElements();
}
