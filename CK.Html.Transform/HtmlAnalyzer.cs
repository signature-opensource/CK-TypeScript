using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CK.Html.Transform;

/// <summary>
/// HtmlAnalyzer swaps between 2 modes:
/// <list type="bullet">
///     <item><term>Text</term>
///     <description>
///     Whitespaces are not handled in Trivias. Trivias are only <see cref="TriviaHeadExtensions.AcceptXmlComment(ref TriviaHead, bool)"/>
///     and <see cref="TriviaHeadExtensions.AcceptXmlCDATA(ref TriviaHead)"/>.
///     Whitespaces appear in <see cref="TokenTypeExtensions.IsHtmlText(TokenType)"/> tokens.
///     These tokens are not unified: multiple Text tokens can appear consecutively because of commants or CDATA trivias and invalid syntax
///     (<c>&lt;div &lt;a &lt;br&gt;</c> is analyzed as "&lt;div " and "&lt;a " texts).
///     </description>
///     </item>
///     <item><term>Element</term>
///     <description>
///     Between <c>&lt;tag</c> and <c>&gt;</c> or <c>/&gt;</c> and when there is at least one attribute, whitespaces trivias are handled normally
///     and attached to <see cref="TokenType.GenericIdentifier"/> (for the attribute name), <see cref="TokenType.Equals"/> and <see cref="TokenType.GenericIdentifier"/>
///     or <see cref="TokenType.GenericString"/> (for the potential attribute value).
///     <para>
///     In this mode, no comment nor CDATA are analyzed.
///     </para>
///     </description>
///     </item>
/// </list>
/// </summary>
public sealed partial class HtmlAnalyzer : TargetLanguageAnalyzer
{
    readonly List<(Token Start, int Index)> _startingTokens;

    /// <summary>
    /// Initialize a new HtmlAnalyzer.
    /// </summary>
    public HtmlAnalyzer()
        : base( HtmlLanguage._languageName, handleWhiteSpaceTrivias: false  )
    {
        _startingTokens = new List<(Token,int)>();
    }

    /// <summary>
    /// When <see cref="Analyzer.HandleWhiteSpaceTrivias"/> is false, this
    /// calls <see cref="TriviaHeadExtensions.AcceptXmlComment(ref TriviaHead, bool)"/>
    /// and <see cref="TriviaHeadExtensions.AcceptXmlCDATA(ref TriviaHead)"/>.
    /// </summary>
    /// <param name="c"></param>
    protected override void ParseTrivia( ref TriviaHead c )
    {
        if( !HandleWhiteSpaceTrivias )
        {
            c.AcceptXmlComment();
            c.AcceptXmlCDATA();
        }
    }

    /// <summary>
    /// Do the hard work of handling spans of text as <see cref="HtmlTokenType.Text"/>.
    /// </summary>
    /// <param name="head">The head to analyze.</param>
    /// <returns>The low level token.</returns>
    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        int iS = 0;

        if( HandleWhiteSpaceTrivias )
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
        // We are not handling whitespaces here, so if we are on the start of a comment,
        // emit the text: the comment (or the CDATA) will be handled as a trivia. 
        if( c == '!' )
        {
            return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS - 1 );
        }
        bool isEnding = c == '/';
        if( !isEnding )
        {
            if( !char.IsAsciiLetter( c ) ) goto text;
            // If there is text before the <[A-Za-z], emit it. The tag will be processed next time.
            if( iS > 1 ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS - 1 );
        }
        else
        {
            if( iS >= head.Length - 1 ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, head.Length );
            c = head[++iS];
            if( !char.IsAsciiLetter( c ) ) goto text;
            // If there is text before the </[A-Za-z], emit it. The tag will be processed next time.
            if( iS > 2 ) return new LowLevelToken( (TokenType)HtmlTokenType.Text, iS - 1 );
        }
        // In a tag name: <[a-zA-Z] or a closing </[a-zA-Z].
        // Extremely permissive here.
        // See https://blog.jim-nielsen.com/2023/validity-of-custom-element-tag-names/
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
        HandleWhiteSpaceTrivias = true;
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
            HandleWhiteSpaceTrivias = false;
            return new LowLevelToken( (TokenType)HtmlTokenType.EndTokenTag, 2 );
        }
        if( c == '>' )
        {
            HandleWhiteSpaceTrivias = false;
            return new LowLevelToken( TokenType.GreaterThan, 1 );
        }
        if( c == '=' ) return new LowLevelToken( TokenType.Equals, 1 );
        if( c == '\'' || c == '"' ) return LowLevelToken.BasicallyReadQuotedString( head );
        // Attribute name or value is an identifier: stops at the first whitespace or one of the
        // other character that have a meaning.
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

    /// <inheritdoc/>
    public override AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        _startingTokens.Clear();
        return base.Parse( text );
    }

    /// <inheritdoc/>
    protected override void DoParse( ref TokenizerHead head )
    {
        while( head.EndOfInput != null )
        {
            var token = head.AcceptLowLevelTokenOrNone();
            switch( (int)token.TokenType )
            {
                case (int)HtmlTokenType.StartingEmptyElement:
                case (int)HtmlTokenType.StartingVoidElement:
                case (int)HtmlTokenType.StartingTag:
                    _startingTokens.Add( (token, head.LastTokenIndex) );
                    break;
                case (int)TokenType.GreaterThan when _startingTokens[^1].Start.TokenType == (TokenType)HtmlTokenType.StartingVoidElement:
                case (int)HtmlTokenType.EndTokenTag:
                    Throw.DebugAssert( _startingTokens.Count > 0 );
                    int startIndex = _startingTokens[^1].Index;
                    _startingTokens.RemoveAt( _startingTokens.Count - 1 );
                    head.AddSpan( new HtmlElementSpan( startIndex, head.LastTokenIndex + 1 ) );
                    break;
                case (int)HtmlTokenType.EndingTag:
                    var name = token.GetHtmlTagName();
                    Throw.DebugAssert( name.Length > 0 );
                    for( int i = _startingTokens.Count - 1; i >= 0; i-- )
                    {
                        var start = _startingTokens[i];
                        var eName = start.Start.GetHtmlTagName();
                        if( eName.Equals( name, StringComparison.OrdinalIgnoreCase ) )
                        {
                            for( int j = _startingTokens.Count - 1; j > i; j-- )
                            {
                                var closingStart = _startingTokens[j].Index;
                                head.AddSpan( new HtmlElementSpan( closingStart, head.LastTokenIndex ) );
                            }
                            head.AddSpan( new HtmlElementSpan( start.Index, head.LastTokenIndex + 1 ) );
                            _startingTokens.RemoveRange( i, _startingTokens.Count - i );
                            break;
                        }
                    }
                    // If we didn't find a matching element name, we let the unmatched closing tag as-is.
                    break;
                // We don't create a 1-length span for <tag/> or <br> (void elements) without attributes.
                // The token is enough.
                //
                // case (int)HtmlTokenType.EmptyElement:
                // case (int)HtmlTokenType.EmptyVoidElement:
                //    head.AddSourceSpan( new HtmlElementSpan( head.LastTokenIndex, head.LastTokenIndex + 1 ) );
                //    break;
                default:
                    Throw.DebugAssert( token.TokenType is TokenType.GenericIdentifier
                                                       or TokenType.Equals
                                                       or TokenType.GenericString
                                                       or TokenType.GreaterThan
                                                       or (TokenType)HtmlTokenType.Text
                                                       or (TokenType)HtmlTokenType.EmptyElement
                                                       or (TokenType)HtmlTokenType.EmptyVoidElement );
                    break;
            }
        }
    }

   
    [GeneratedRegex( "^(area|base|br|col|embed|hr|img|input|link|meta|source|track|wbr)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture )]
    private static partial Regex VoidElements();

}
