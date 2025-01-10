using CK.Core;
using CK.Transform.Core;
using System.Buffers;
using System;
using System.Collections.Generic;

namespace CK.TypeScript.Transform;

sealed partial class TypeScriptAnalyzer // Scanner
{
    static ReadOnlySpan<char> _regexFlags => "dgimsuvy";
    static ReadOnlySpan<char> _binaryDigits => "01_";
    static ReadOnlySpan<char> _octalDigits => "01234567_";
    static ReadOnlySpan<char> _decimalDigits => "0123456789_";
    static ReadOnlySpan<char> _hexadecimalDigits => "0123456789ABSDEFabcdef_";

    static SearchValues<char> _binary = SearchValues.Create( _binaryDigits );
    static SearchValues<char> _octal = SearchValues.Create( _octalDigits );
    static SearchValues<char> _decimal = SearchValues.Create( _decimalDigits );
    static SearchValues<char> _hexadecimal = SearchValues.Create( _hexadecimalDigits );

    // Keeps the brace depth of interpolated starts.
    readonly Stack<int> _interpolated;
    int _braceDepth;

    /// <summary>
    /// Handles interpolated strings and /regular expressions/.
    /// <see cref="LowLevelTokenize(ReadOnlySpan{char})"/> handles numbers, "string", 'string', identifiers (including @identifier and #identifier)
    /// and `start of interpolated string` (GenericInterpolatedStringStart).
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>Next token.</returns>
    Token Scan( ref TokenizerHead head )
    {
        switch( head.LowLevelTokenType )
        {
            case TokenType.GenericInterpolatedStringStart:
                head.AcceptLowLevelToken();
                _interpolated.Push( ++_braceDepth );
                break;
            case TokenType.OpenBrace:
                head.AcceptLowLevelToken();
                ++_braceDepth;
                break;
            case TokenType.CloseBrace:
                if( _interpolated.TryPeek( out var depth ) && depth == _braceDepth )
                {
                    var t = ReadInterpolatedSegment( head.Head, false );
                    if( t.TokenType == TokenType.GenericInterpolatedStringEnd )
                    {
                        --_braceDepth;
                    }
                    _interpolated.Pop();
                    head.AcceptToken( t.TokenType, t.Length );
                }
                else
                {
                    if( --_braceDepth < 0 )
                    {
                        return head.CreateHardError( "Unbalanced {{brace}." );
                    }
                    head.AcceptLowLevelToken();
                }
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
                return head.CreateHardError( "Unrecognized token." );
            case TokenType.EndOfInput:
                Throw.DebugAssert( head.EndOfInput is not null );
                if( _braceDepth > 0 )
                {
                    return head.CreateHardError( "Missing closing '}'." );
                }
                return head.EndOfInput;
            default:
                head.AcceptLowLevelToken();
                break;
        }
        return head.LastToken;
    }

    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        Throw.DebugAssert( head.Length > 0 );
        // Since numbers can start with a dot, let's start with numbers.
        var t = TryReadNumber( head );
        if( t.TokenType != TokenType.None ) return t;
        // It's not a number. Use the standard basic tokens.
        // If it's a / or a /= then it may be a /regex/ but this cannot be determined here.
        t = LowLevelToken.GetBasicTokenType( head );
        if( t.TokenType != TokenType.None )
        {
            if( head.Length > 1 )
            {
                // Handle private #field and @decorator as identifiers.
                if( t.TokenType is TokenType.Hash or TokenType.AtSign )
                {
                    return ReadIdentifier( head, 1 );
                }
                if( t.TokenType is TokenType.DoubleQuote or TokenType.SingleQuote )
                {
                    return ReadString( head );
                }
                if( t.TokenType is TokenType.BackTick )
                {
                    return ReadInterpolatedSegment( head, true );
                }
            }
            return t;
        }
        // Anything else must be an identifier.
        return ReadIdentifier( head, 0 );

        static LowLevelToken ReadIdentifier( ReadOnlySpan<char> head, int iS )
        {
            if( !IsIdentifierStart( head[iS] ) ) return default;
            while( ++iS < head.Length && IsIdentifierPart( head[iS] ) ) ;
            return new LowLevelToken( TokenType.GenericIdentifier, iS );
        }

        // Real code: https://github.com/microsoft/TypeScript/blob/main/src/compiler/scanner.ts#L1232
        // This one is far more simpler as it accepts syntaxically invalid numbers (but no more than numbers).
        static LowLevelToken TryReadNumber( ReadOnlySpan<char> head )
        {
            static void EatDigits( ref int iS, ReadOnlySpan<char> head, SearchValues<char> set )
            {
                while( ++iS < head.Length && set.Contains( head[iS] ) ) ;
            }

            Throw.DebugAssert( head.Length > 0 );
            bool isFloat = false;
            var c = head[0];
            int iS = 0;
            if( c == '0' )
            {
                iS = 1;
                if( head.Length > 1 )
                {
                    c = head[1];
                    if( c == 'x' || c == 'X' )
                    {
                        // We don't care if there is no digit.
                        EatDigits( ref iS, head, _hexadecimal );
                        // BigInt notation.
                        if( iS < head.Length && head[iS] == 'n' ) ++iS;
                        return new LowLevelToken( TokenType.GenericNumber, iS );
                    }
                    if( c == 'b' || c == 'B' )
                    {
                        // We don't care if there is no digit.
                        EatDigits( ref iS, head, _binary );
                        // BigInt notation.
                        if( iS < head.Length && head[iS] == 'n' ) ++iS;
                        return new LowLevelToken( TokenType.GenericNumber, iS );
                    }
                    if( c == 'o' || c == 'O' )
                    {
                        // We don't care if there is no digit.
                        EatDigits( ref iS, head, _octal );
                        // BigInt notation.
                        if( iS < head.Length && head[iS] == 'n' ) ++iS;
                        return new LowLevelToken( TokenType.GenericNumber, iS );
                    }
                }
                EatDigits( ref iS, head, _decimal );
            }
            else if( c == '.' )
            {
                isFloat = true;
                EatDigits( ref iS, head, _decimal );
                // No digit: we have a Dot but don't return it here because
                // don't want to reproduce the DotDot and DotDotDot parsing here.
                if( iS == 1 ) return default;
            }
            else
            {
                if( !char.IsAsciiDigit( c ) ) return default;
                EatDigits( ref iS, head, _decimal );
            }
            if( iS < head.Length )
            {
                c = head[iS];
                if( !isFloat && c == '.' )
                {
                    EatDigits( ref iS, head, _decimal );
                    if( iS == head.Length ) return new LowLevelToken( TokenType.GenericNumber, iS );
                    c = head[iS];
                    isFloat = true;
                }
                if( c == 'e' || c == 'E' )
                {
                    if( ++iS == head.Length ) return new LowLevelToken( TokenType.GenericNumber, iS );
                    c = head[iS];
                    if( c != '+' && c != '-' && !char.IsAsciiDigit( c ) ) return new LowLevelToken( TokenType.GenericNumber, iS );
                    EatDigits( ref iS, head, _decimal );
                    isFloat = true;
                }
            }
            if( !isFloat )
            {
                if( ++iS == head.Length ) return new LowLevelToken( TokenType.GenericNumber, iS );
                // BigInt notation.
                if( head[iS] == 'n' ) ++iS;
            }
            return new LowLevelToken( TokenType.GenericNumber, iS );
        }

        // Very simple because here again we don't validate anything. The \ escaper simply skips the next char.
        // This handles the javascript line continuation and any escape sequence by simply ignoring it.
        static LowLevelToken ReadString( ReadOnlySpan<char> head )
        {
            var q = head[0];
            int iS = 0;
            bool escape = false;
            for(; ; )
            {
                if( ++iS == head.Length ) return new LowLevelToken( TokenType.ErrorUnterminatedString, iS );
                if( escape ) continue;
                var c = head[iS];
                if( c == q ) return new LowLevelToken( TokenType.GenericString, iS + 1 );
                escape = c == '\\';
            }
        }
    }

    static LowLevelToken ReadInterpolatedSegment( ReadOnlySpan<char> head, bool start )
    {
        int iS = 0;
        bool escape = false;
        bool mayBeHole = false;
        for(; ; )
        {
            if( ++iS == head.Length ) return new LowLevelToken( TokenType.ErrorUnterminatedString, iS );
            var c = head[iS];
            if( escape ) continue;
            if( mayBeHole && c == '{' )
            {
                return new LowLevelToken( start ? TokenType.GenericInterpolatedStringStart : TokenType.GenericInterpolatedStringSegment, iS + 1 );
            }
            if( c == '`' ) return new LowLevelToken( start ? TokenType.GenericString : TokenType.GenericInterpolatedStringEnd, iS + 1 );
            escape = c == '\\';
            mayBeHole = !escape && c == '$';
        }
    }

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

    static bool IsIdentifierStart( char c )
    {
        return char.IsAsciiLetter( c )
                || c == '$'
                || c == '_'
                || c > 255 && IsUnicodeIdentifierStart( c );

        // As per ECMAScript Language Specification 5th Edition, Section 7.6: Identifier Names and Identifiers
        //    IdentifierStart :: Can contain Unicode 6.2  categories “Uppercase letter (Lu)”, “Lowercase letter (Ll)”, “Titlecase letter (Lt)”, 
        //                       “Modifier letter (Lm)”, “Other letter (Lo)”, or “Letter number (Nl)”.
        //    IdentifierPart :: Can contain IdentifierStart + Unicode 6.2  categories “Non-spacing mark (Mn)”, “Combining spacing mark (Mc)”, 
        //                       “Decimal number (Nd)”, “Connector punctuation (Pc)”, <ZWNJ>, or <ZWJ>.
        static bool IsUnicodeIdentifierStart( char c )
        {
            var cat = char.GetUnicodeCategory( c );
            return cat == System.Globalization.UnicodeCategory.UppercaseLetter
                    || cat == System.Globalization.UnicodeCategory.LowercaseLetter
                    || cat == System.Globalization.UnicodeCategory.TitlecaseLetter
                    || cat == System.Globalization.UnicodeCategory.ModifierLetter
                    || cat == System.Globalization.UnicodeCategory.OtherLetter
                    || cat == System.Globalization.UnicodeCategory.LetterNumber;
        }
    }

    static bool IsIdentifierPart( char c )
    {
        return char.IsAsciiLetterOrDigit( c )
                || c == '_'
                || c == '$'
                || c > 255 && IsUnicodeIdentifierPart( c );

        static bool IsUnicodeIdentifierPart( char c )
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

}

