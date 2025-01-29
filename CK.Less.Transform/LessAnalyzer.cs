using CK.Core;
using CK.Transform.Core;
using System.Buffers;

namespace CK.Less.Transform;

public class LessAnalyzer : Tokenizer, IAnalyzer
{
    public string LanguageName => LessLanguage._languageName;

    protected override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeLineComment();
        c.AcceptCLikeStarComment();
    }

    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        Throw.DebugAssert( head.Length > 0 );

        // First, let's try a number (it may start with a minus or a plus and/or a dot).
        // In CSS, +/- are unary operators only when they are followed by a whitespace.
        var t = TryReadNumber( head, 0 );
        if( t.TokenType != TokenType.None ) return t;

        var c = head[0];
        // Handles strings.
        if( c == '\'' || c == '"' )
        {
            // We don't care about @{variable-interpolation} inside a string:
            // the basic read is fine.
            return LowLevelToken.BasicallyReadQuotedString( head );
        }
        // The challenge now is to correctly handle selectors, property names and values...
        // Formally, what's the difference between the 2 'some' below given that value can be a rather
        // complex "expression" that may contain ';' or '{'?
        // div
        // {
        //      some:value;
        //      some:value { }
        // }
        // The only local solution is to rely on the actual set of pseudo-classes and pseudo-elements
        // when handling a ':'.
        // https://developer.mozilla.org/en-US/docs/Web/CSS/Pseudo-classes
        // https://developer.mozilla.org/en-US/docs/Web/CSS/Pseudo-elements
        // A double colon '::' indicates a pseudo-element, unfortunately browsers can accept single ':' for them.
        // (But a '::' is definitely a selector part.)
        //
        // Of course, this tokenization must accept @variable and also @{variable-interpolation} inside a "token".
        // Currently, we use the very basic algorithm below.
        // ':' (or '::') are always independent tokens, they break the "extended identifier token" just like
        // a comma, ;, [, ( or any white space.
        t = ReadExtendedIdentifier( head, c );
        if( t.TokenType != TokenType.None ) return t;

        // If our extended identifier is not matched, fall backs to BasicTokenType.
        // Among basic tokens it seems that only the "$=" is missing.
        // Let it be missing (will be "$" and "=" basic tokens).
        return LowLevelToken.GetBasicTokenType( head );

        static LowLevelToken ReadExtendedIdentifier( ReadOnlySpan<char> head, char c )
        {
            if( !IsValidStart( c ) ) return default;
            int iS = 0;
            while( ++iS < head.Length && IsValidContinuation( head[iS] ) ) ;
            return iS > 1
                     ? new LowLevelToken( TokenType.GenericIdentifier, iS )
                     : default;

            // AsciiLetter, > 0x80 , _ and - (numbers have been handled above) handle basic css identifiers.
            // # handles id selector and https://lesscss.org/#namespaces-and-accessors
            // @ starts a variable or a variable interpolation.
            // . handles class selector.
            // $ handles https://lesscss.org/features/#variables-feature-properties-as-variables-new-
            static bool IsValidStart( char c ) => char.IsAsciiLetter( c ) || c > 0x80 || c == '_' || c == '-'
                                                  || c == '#'
                                                  || c == '@'
                                                  || c == '.'
                                                  || c == '$';

            // Then accepts digits and {} to handle variable interpolation.
            static bool IsValidContinuation( char c ) => IsValidStart( c ) || char.IsAsciiDigit( c ) || c == '{' || c == '}';
        }

        static LowLevelToken TryReadNumber( ReadOnlySpan<char> head, int iS )
        {
            Throw.DebugAssert( iS < head.Length );

            static void EatDigits( ref int iS, ReadOnlySpan<char> head )
            {
                while( ++iS < head.Length && char.IsAsciiDigit( head[iS] ) ) ;
            }

            var c = head[iS];
            if( c == '-' || c == '+' )
            {
                // Don't want to handle +/- token type here.
                if( ++iS == head.Length ) return default;
            }
            bool isFloat = false;
            if( c == '.' )
            {
                isFloat = true;
                int iSaved = iS;
                EatDigits( ref iS, head );
                // No digit: we have a Dot but don't return it here because
                // don't want to reproduce the DotDot and DotDotDot parsing here.
                if( iS == iSaved ) return default;
            }
            else
            {
                if( !char.IsAsciiDigit( c ) ) return default;
                EatDigits( ref iS, head );
            }
            if( iS < head.Length )
            {
                c = head[iS];
                if( !isFloat && c == '.' )
                {
                    EatDigits( ref iS, head );
                    if( iS == head.Length ) return new LowLevelToken( TokenType.GenericNumber, iS );
                    c = head[iS];
                    isFloat = true;
                }
                if( c == 'e' || c == 'E' )
                {
                    if( ++iS == head.Length ) return new LowLevelToken( TokenType.GenericNumber, iS );
                    c = head[iS];
                    if( c != '+' && c != '-' && !char.IsAsciiDigit( c ) ) return new LowLevelToken( TokenType.GenericNumber, iS );
                    EatDigits( ref iS, head );
                    isFloat = true;
                }
            }
            return new LowLevelToken( TokenType.GenericNumber, iS );
        }
    }

    protected override TokenError? Tokenize( ref TokenizerHead head )
    {
        for(; ; )
        {
            if( head.EndOfInput != null ) return null;
            if( head.LowLevelTokenType is TokenType.None )
            {
                return head.CreateHardError( "Unrecognized token." );
            }
            var t = head.AcceptLowLevelToken();
            // Handles @import.
            if( t.Text.Span.Equals( "@import", StringComparison.Ordinal ) )
            {
                var importStatement = ImportStatement.TryMatch( t, ref head );
                Throw.DebugAssert( "TryMatch doesn't add the span.", importStatement == null || importStatement.IsDetached );
                if( importStatement != null ) head.AddSourceSpan( importStatement );
            }
        }
    }

    public AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        Reset( text );
        return Parse();
    }
}
