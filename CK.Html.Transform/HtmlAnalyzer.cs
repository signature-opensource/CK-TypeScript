using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Html.Transform;



public sealed class HtmlAnalyzer : Tokenizer, IAnalyzer
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
        Throw.DebugAssert( head[iS] == '<' );
        if( ++iS == head.Length ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );
        if( !char.IsAsciiLetter( head[iS] ) ) goto text;
        if( iS > 1 ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );

        // In a tag name: <[a-zA-Z]
        bool isWhitespace = false;
        var c = head[iS];
        while( !(isWhitespace = char.IsWhiteSpace( c )) && c != '/' && c != '>' && c != '<' )
        {
            // End of input: emit the "<tag" as text.
            if( ++iS == head.Length ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );
            c = head[iS];
        }
        int endTagName = iS;
        if( isWhitespace )
        {
            do
            {
                // End of input: emit the "<tag" (or "<tag     ") as text.
                if( ++iS == head.Length ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );
                c = head[iS];
            }
            while( char.IsWhiteSpace( c ) && c != '/' && c != '>' && c != '<' );
        }
        if( c == '>' ) 
        {
            // StartingElement (without attributes).
            // May be one of the a void element (<br> or <area  >) but we don't handle this here.
            return new LowLevelToken( (TokenType)HtmlTokenType.StartingElement, iS );
        }
        if( c == '/' ) // Should now be '>'... but who knows...
        {
            // End of input: emit the "<tag /" as text.
            if( ++iS == head.Length ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );
            // Invalid "<tag /x" where 'x' is not '>': consider it text.
            if( head[iS] != '>' ) goto text;
            return new LowLevelToken( (TokenType)HtmlTokenType.StartingElement, iS );
        }
        if( c == '<' )
        {
            // "<Ouch <": emits "<Ouch " as text.
            return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS );
        }
        // We are on the start of an attribute name. The next token (GenericIdentifier) will have the whitespace as a Trivia,
        // and the following tokens use regular whitespace trivia handling.
        _inAttribute = true;
        return new LowLevelToken( (TokenType)HtmlTokenType.StartingElement, endTagName );
    }

    LowLevelToken LowLevelTokenizeAttribute( ReadOnlySpan<char> head )
    {
        var c = head[0];
        if( c == '<' )
        {
            // Here we give up: error.
            return new LowLevelToken( (TokenType)HtmlTokenType.StartingElement | TokenType.ErrorClassBit, 1 );
        }
        if( c == '/' )
        {
            if( head.Length == 1 || head[1] != '>' )
            {
                // Here we give up: error.
                return new LowLevelToken( (TokenType)HtmlTokenType.ClosingTag | TokenType.ErrorClassBit, 1 );
            }
            _inAttribute = false;
            return new LowLevelToken( (TokenType)HtmlTokenType.ClosingTag, 2 );
        }
        if( c == '>' )
        {
            _inAttribute = false;
            return new LowLevelToken( (TokenType)HtmlTokenType.ClosingTag, 1 );
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
}
