using CK.Core;
using System;

namespace CK.Transform.Core.Tests;

sealed partial class TestAnalyzer
{
    internal sealed class Scanner : ITokenScanner
    {
        int _braceDepth;
        int _bracketDepth;
        int _parenDepth;
        readonly bool _useSourceSpanBraceAndBrackets;

        public Scanner( bool useSourceSpanBraceAndBrackets )
        {
            _useSourceSpanBraceAndBrackets = useSourceSpanBraceAndBrackets;
        }

        internal int BraceDepth => _braceDepth;

        internal int BracketDepth => _bracketDepth;

        internal int ParenDepth => _parenDepth;

        internal void Reset()
        {
            _braceDepth = 0;
            _bracketDepth = 0;
            _parenDepth = 0;
        }

        internal void Parse( ref TokenizerHead head  )
        {
            while( head.EndOfInput == null )
            {
                var t = GetNextToken( ref head );
                if( !t.TokenType.IsError() )
                {
                    HandleKnownSpan( ref head, t );
                }
            }
        }

        internal SourceSpan? HandleKnownSpan( ref TokenizerHead head, Token t )
        {
            Throw.DebugAssert( t is not TokenError );
            if( _useSourceSpanBraceAndBrackets )
            {
                if( t.TokenType is TokenType.OpenBrace )
                {
                    return BraceSpan.Match( this, ref head, t );
                }
                if( t.TokenType is TokenType.OpenBracket )
                {
                    return BracketSpan.Match( this, ref head, t );
                }
                if( t.TokenType is TokenType.OpenParen )
                {
                    return ParenSpan.Match( this, ref head, t );
                }
            }
            return null;
        }

        public Token GetNextToken( ref TokenizerHead head )
        {
            if( head.EndOfInput != null )
            {
                return head.EndOfInput;
            }
            switch( head.LowLevelTokenType )
            {
                case TokenType.OpenBrace:
                    head.AcceptLowLevelToken();
                    ++_braceDepth;
                    break;
                case TokenType.CloseBrace:
                    if( _braceDepth == 0 )
                    {
                        head.AppendUnexpectedToken();
                    }
                    else
                    {
                        --_braceDepth;
                        head.AcceptLowLevelToken();
                    }
                    break;
                case TokenType.OpenBracket:
                    head.AcceptLowLevelToken();
                    ++_bracketDepth;
                    break;
                case TokenType.CloseBracket:
                    if( _bracketDepth == 0 )
                    {
                        head.AppendUnexpectedToken();
                    }
                    else
                    {
                        --_bracketDepth;
                        head.AcceptLowLevelToken();
                    }
                    break;
                case TokenType.OpenParen:
                    head.AcceptLowLevelToken();
                    ++_parenDepth;
                    break;
                case TokenType.CloseParen:
                    if( _parenDepth == 0 )
                    {
                        head.AppendUnexpectedToken();
                    }
                    else
                    {
                        --_parenDepth;
                        head.AcceptLowLevelToken();
                    }
                    break;
                case TokenType.DoubleQuote:
                    RawString.Match( ref head );
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
                default:
                    // Handle BUG identifier by adding an error with a 0 text length and NOT accepting
                    // the low level token.
                    if( head.LowLevelTokenText.Equals( "BUG", StringComparison.Ordinal ) )
                    {
                        head.AppendError( "Expected BUG!", 0 );
                    }
                    else
                    {
                        head.AcceptLowLevelTokenOrNone();
                    }
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

}
