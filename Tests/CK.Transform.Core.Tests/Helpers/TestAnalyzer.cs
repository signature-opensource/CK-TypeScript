using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.Core.Tests.Helpers;

sealed class TestAnalyzer : Tokenizer, IAnalyzer
{
    public string LanguageName => "Test";

    /// <summary>
    /// Exposes this Tokenizer as an Analyzer.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        Reset( text );
        return Parse();
    }

    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        // Handles all the basic token types.
        var t = LowLevelToken.GetBasicTokenType( head );
        if( t.TokenType == TokenType.None )
        {
            // If it's not a basic token type, then handles a "Ascii letter[Ascii letter or digit or _]*" identifier.
            if( char.IsAsciiLetter( head[0] ) )
            {
                int iS = 0;
                while( ++iS < head.Length && (head[iS] == '_' || char.IsAsciiLetterOrDigit( head[iS] )) ) ;
                t = new LowLevelToken( TokenType.GenericIdentifier, iS );
            }
        }
        return t;
    }

    /// <summary>
    /// Accepts nested <c>/* ... /* ... */ ... */</c> comments, classical <c>// line comments</c>
    /// and <c>&lt;!-- ... --&gt;</c> xml comments.
    /// </summary>
    /// <param name="c"></param>
    protected override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeRecursiveStarComment();
        c.AcceptCLikeLineComment();
        c.AcceptXmlComment();
    }

    protected override TokenError? Tokenize( ref TokenizerHead head )
    {
        for(; ; )
        {
            var t = Scan( ref head );
            if( t.TokenType == TokenType.EndOfInput ) return null;
        }
    }

    static Token Scan( ref TokenizerHead head )
    {
        switch( head.LowLevelTokenType )
        {
            case TokenType.DoubleQuote:
                RawString.TryMatch( ref head );
                Throw.DebugAssert( head.LastToken is RawString or TokenError );
                break;
            case TokenType.Slash or TokenType.SlashEquals:
                if( head.LastToken != null && head.LastToken.TokenType is not TokenType.GenericIdentifier
                                                                      and not TokenType.GenericNumber
                                                                      and not TokenType.GenericString
                                                                      and not TokenType.GenericRegularExpression
                                                                      and not TokenType.PlusPlus
                                                                      and not TokenType.MinusMinus
                                                                      and not TokenType.CloseParen
                                                                      and not TokenType.CloseBrace
                                                                      and not TokenType.CloseBracket )
                {
                    var t = TryParseRegex( new LowLevelToken( head.LowLevelTokenType, head.LowLevelTokenText.Length ), head.Head );
                    head.AcceptToken( t.TokenType, t.Length );
                }
                else
                {
                    head.AcceptLowLevelToken();
                }
                break;
            case TokenType.None:
                head.AppendUnexpectedToken();
                break;
            case TokenType.EndOfInput:
                Throw.DebugAssert( head.EndOfInput is not null );
                return head.EndOfInput;
            default:
                head.AcceptLowLevelToken();
                break;
        }
        return head.LastToken;

    }

    static ReadOnlySpan<char> _regexFlags => "dgimsuvy";

    static LowLevelToken TryParseRegex( LowLevelToken slash, ReadOnlySpan<char> head )
    {
        Throw.DebugAssert( slash.TokenType is TokenType.Slash or TokenType.SlashEquals );
        // From https://github.com/microsoft/TypeScript/blob/main/src/compiler/scanner.ts#L2466.
        int iS = slash.Length;
        var inEscape = false;
        // Although nested character classes are allowed in Unicode Sets mode,
        // an unescaped slash is nevertheless invalid even in a character class in any Unicode mode.
        // This is indicated by Section 12.9.5 Regular Expression Literals of the specification,
        // where nested character classes are not considered at all. (A `[` RegularExpressionClassChar
        // does nothing in a RegularExpressionClass, and a `]` always closes the class.)
        // Additionally, parsing nested character classes will misinterpret regexes like `/[[]/`
        // as unterminated, consuming characters beyond the slash. (This even applies to `/[[]/v`,
        // which should be parsed as a well-terminated regex with an incomplete character class.)
        // Thus we must not handle nested character classes in the first pass.
        var inCharacterClass = false;
        for(; ; )
        {
            char c;
            if( iS == head.Length || (c = head[iS]) == '\n' ) return slash;
            if( inEscape )
            {
                // Parsing an escape character;
                // reset the flag and just advance to the next char.
                inEscape = false;
            }
            else if( c == '/' && !inCharacterClass )
            {
                // A slash within a character class is permissible,
                // but in general it signals the end of the regexp literal.
                break;
            }
            else if( c == '[' )
            {
                inCharacterClass = true;
            }
            else if( c == '\\' )
            {
                inEscape = true;
            }
            else if( c == ']' )
            {
                inCharacterClass = false;
            }
            iS++;
        }
        // Consume the slash character and forward past the last allowed flag.
        while( ++iS < head.Length && _regexFlags.Contains( head[iS] ) ) ;
        return new LowLevelToken( TokenType.GenericRegularExpression, iS );
    }

}
