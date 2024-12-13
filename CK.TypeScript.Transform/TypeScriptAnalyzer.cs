using CK.Transform.Core;
using System;
using System.Security.Cryptography;
using static CK.Core.ActivityMonitorErrorCounter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CK.TypeScript.Transform;

sealed class TypeScriptAnalyzer : Analyzer
{
    public override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        var c = head[0];
        // If any single char node type is also s valid identifier start, the
        // identifier must be considered in priority.
        if( IsIdentifierStart( c ) )
        {
            int iS = 0;
            while( ++iS < head.Length && IsIdentifierPart( head[iS] ) ) ;
            return new LowLevelToken( NodeType.GenericIdentifier, iS );
        }
        var knownSingle = NodeTypeExtensions.GetSingleCharType( c );
        if( knownSingle != NodeType.None )
        {
            if( head.Length > 1 )
            {
                c = head[1];
                switch( knownSingle )
                {
                    case NodeType.Ampersand:
                        if( c == '&' ) return new LowLevelToken( NodeType.AmpersandAmpersand, 2 );
                        if( c == '=' ) return new LowLevelToken( NodeType.AmpersandEquals, 2 );
                        break;
                    case NodeType.Asterisk:
                        if( c == '=' ) return new LowLevelToken( NodeType.AsteriskEquals, 2 );
                        break;
                    case NodeType.Bar:
                        if( c == '|' ) return new LowLevelToken( NodeType.BarBar, 2 );
                        if( c == '=' ) return new LowLevelToken( NodeType.BarEquals, 2 );
                        break;
                    case NodeType.Caret:
                        if( c == '=' ) return new LowLevelToken( NodeType.CaretEquals, 2 );
                        break;
                    case NodeType.Dot:
                        if( c == '.' )
                        {
                            if( head.Length > 2 && head[2] == '.' )
                            {
                                return new LowLevelToken( NodeType.DotDotDot, 3 );
                            }
                            return new LowLevelToken( NodeType.DotDot, 2 );
                        }
                        break;
                    case NodeType.Equals:
                        if( c == '=' )
                        {
                            if( head.Length > 2 && head[2] == '=' )
                            {
                                return new LowLevelToken( NodeType.EqualsEqualsEquals, 3 );
                            }
                            return new LowLevelToken( NodeType.EqualsEquals, 2 );
                        }
                        if( c == '>' )
                        {
                            return new LowLevelToken( NodeType.EqualsGreaterThan, 2 );
                        }
                        break;
                    case NodeType.Exclamation:
                        if( c == '=' )
                        {
                            if( head.Length > 2 && head[2] == '=' )
                            {
                                return new LowLevelToken( NodeType.ExclamationEqualsEquals, 3 );
                            }
                            return new LowLevelToken( NodeType.ExclamationEquals, 2 );
                        }
                        break;
                    case NodeType.Percent:
                        if( c == '=' ) return new LowLevelToken( NodeType.PercentEquals, 2 );
                        break;
                    case NodeType.Minus:
                        if( c == '=' ) return new LowLevelToken( NodeType.MinusEquals, 2 );
                        if( c == '-' ) return new LowLevelToken( NodeType.MinusMinus, 2 );
                        break;
                    case NodeType.Plus:
                        if( c == '=' ) return new LowLevelToken( NodeType.PlusEquals, 2 );
                        if( c == '+' ) return new LowLevelToken( NodeType.PlusPlus, 2 );
                        break;
                    case NodeType.Slash:
                        if( c == '=' ) return new LowLevelToken( NodeType.SlashEquals, 2 );
                        break;
                    case NodeType.LessThan:
                        if( c == '<' )
                        {
                            if( head.Length > 2 )
                            {
                                if( head[2] == '<' ) return new LowLevelToken( NodeType.LessThanLessThanLessThan, 3 );
                                if( head[2] == '=' ) return new LowLevelToken( NodeType.LessThanLessThanEquals, 3 );
                            }
                            return new LowLevelToken( NodeType.LessThanLessThan, 2 );
                        }
                        if( c == '=' )
                        {
                            return new LowLevelToken( NodeType.LessThanEquals, 2 );
                        }
                        break;
                    case NodeType.GreaterThan:
                        if( c == '>' )
                        {
                            if( head.Length > 2 )
                            {
                                if( head[2] == '>' )
                                {
                                    if( head.Length > 3 )
                                    {
                                        if( head[3] == '=' ) return new LowLevelToken( NodeType.GreaterThanGreaterThanGreaterThanEquals, 4);
                                    }
                                    return new LowLevelToken( NodeType.GreaterThanGreaterThanGreaterThan, 3 );
                                }
                                if( head[2] == '=' ) return new LowLevelToken( NodeType.GreaterThanGreaterThanEquals, 3 );
                            }
                            return new LowLevelToken( NodeType.GreaterThanGreaterThan, 2 );
                        }
                        if( c == '=' )
                        {
                            return new LowLevelToken( NodeType.GreaterThanEquals, 2 );
                        }
                        break;

                }
            }
            return new LowLevelToken( knownSingle, 1 );
        }
        return default;

        // As per ECMAScript Language Specification 5th Edition, Section 7.6: Identifier Names and Identifiers
        //    IdentifierStart :: Can contain Unicode 6.2  categories “Uppercase letter (Lu)”, “Lowercase letter (Ll)”, “Titlecase letter (Lt)”, 
        //                       “Modifier letter (Lm)”, “Other letter (Lo)”, or “Letter number (Nl)”.
        //    IdentifierPart :: Can contain IdentifierStart + Unicode 6.2  categories “Non-spacing mark (Mn)”, “Combining spacing mark (Mc)”, 
        //                       “Decimal number (Nd)”, “Connector punctuation (Pc)”, <ZWNJ>, or <ZWJ>.
        static bool IsIdentifierStart( char c )
        {
            var cat = char.GetUnicodeCategory( c );
            return cat == System.Globalization.UnicodeCategory.UppercaseLetter
                   || cat == System.Globalization.UnicodeCategory.LowercaseLetter
                   || cat == System.Globalization.UnicodeCategory.TitlecaseLetter
                   || cat == System.Globalization.UnicodeCategory.ModifierLetter
                   || cat == System.Globalization.UnicodeCategory.OtherLetter
                   || cat == System.Globalization.UnicodeCategory.LetterNumber;
        }

        static bool IsIdentifierPart( char c )
        {
            var cat = char.GetUnicodeCategory( c );
            return cat == System.Globalization.UnicodeCategory.UppercaseLetter
                   || cat == System.Globalization.UnicodeCategory.LowercaseLetter
                   || cat == System.Globalization.UnicodeCategory.TitlecaseLetter
                   || cat == System.Globalization.UnicodeCategory.ModifierLetter
                   || cat == System.Globalization.UnicodeCategory.OtherLetter
                   || cat == System.Globalization.UnicodeCategory.LetterNumber
                   || cat == System.Globalization.UnicodeCategory.NonSpacingMark
                   || cat == System.Globalization.UnicodeCategory.SpacingCombiningMark
                   || cat == System.Globalization.UnicodeCategory.DecimalDigitNumber
                   || cat == System.Globalization.UnicodeCategory.ConnectorPunctuation
                   || c == '\u200C'  // Zero-width non-joiner
                   || c == '\u200D'; // Zero-width joiner
        }
    }

    public override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeLineComment();
        c.AcceptCLikeStarComment();
    }

    protected override IAbstractNode? Parse( ref ParserHead head )
    {
        throw new NotImplementedException();
    }
}

public static class NumberScanner
{
}

