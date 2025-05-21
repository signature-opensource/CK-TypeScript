using CK.Core;
using System;

namespace CK.Transform.Core.Tests;

/// <summary>
/// Basic analyser for Test language.
/// Identifiers are "Ascii letter[Ascii letter or digit or _]*" (<see cref="TokenType.GenericIdentifier"/>).
/// Numbers are parsed in a "standard" way with integers and floats (<see cref="TokenType.GenericNumber"/>).
/// "Double quote" strings are <see cref="RawString"/> (<see cref="TokenType.GenericString"/>).
/// Javascript-like regex are handled (<see cref="TokenType.GenericRegularExpression"/>).
/// <para>
/// Spans are <see cref="BraceSpan"/> and <see cref="BracketSpan"/>.
/// </para>
/// <para>
/// The BUG identifier appends an error without forwarding the head: the parser blocks
/// on it. This is detected and eventually an exception is thrown by the TokenizerHead.
/// </para>
/// </summary>
sealed partial class TestAnalyzer : TargetLanguageAnalyzer
{
    readonly Scanner _reusableScanner;

    public TestAnalyzer()
        : base( TestLanguage._langageName )
    {
        _reusableScanner = new Scanner();
    }

    /// <summary>
    /// Overridden to reset the internal scanner state.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>The parsing result.</returns>
    public override AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        _reusableScanner.Reset();
        return base.Parse( text );
    }

    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        // Since numbers can start with a dot, let's start with numbers.
        var t = TryReadNumber( head );
        if( t.TokenType != TokenType.None ) return t;
        // Handles all the basic token types.
        t = LowLevelToken.GetBasicTokenType( head );
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

        static LowLevelToken TryReadNumber( ReadOnlySpan<char> head )
        {
            static void EatDigits( ref int iS, ReadOnlySpan<char> head )
            {
                Throw.DebugAssert( iS < head.Length );
                while( ++iS < head.Length && char.IsAsciiDigit( head[iS] ) ) ;
            }

            Throw.DebugAssert( head.Length > 0 );
            bool isFloat = false;
            var c = head[0];
            int iS = 0;
            if( c == '.' )
            {
                isFloat = true;
                EatDigits( ref iS, head );
                // No digit: we have a Dot but don't return it here because
                // don't want to reproduce the DotDot and DotDotDot parsing here.
                if( iS == 1 ) return default;
            }
            else
            {
                if( !char.IsAsciiDigit( c ) ) return default;
                EatDigits( ref iS, head );
            }
            if( iS == head.Length ) return new LowLevelToken( TokenType.GenericNumber, iS );
            Throw.DebugAssert( iS < head.Length );

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
                if( iS == head.Length ) return new LowLevelToken( TokenType.GenericNumber, iS );
                c = head[iS];
                isFloat = true;
            }
            // BigInt notation.
            if( !isFloat && c == 'n' ) ++iS;
            return new LowLevelToken( TokenType.GenericNumber, iS );
        }
    }

    /// <summary>
    /// Accepts nested <c>/* ... /* ... */ ... */</c> comments and classical <c>// line comments</c>.
    /// </summary>
    /// <param name="c"></param>
    protected override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeRecursiveStarComment();
        c.AcceptCLikeLineComment();
    }

    protected override void DoParse( ref TokenizerHead head ) => _reusableScanner.Parse( ref head );

    protected override object ParseSpanSpec( BalancedString tokenSpec )
    {
        var spanSpec = tokenSpec.InnerText.Trim();
        if( spanSpec.Length > 0 )
        {
            return spanSpec switch
            {
                "braces" => new SpanTypeOperator( typeof( BraceSpan ), "{braces}" ),
                "brackets" => new SpanTypeOperator( typeof( BracketSpan ), "{brackets}" ),
                "^braces" => new CoveringSpanTypeOperator( typeof( BraceSpan ), "{^braces}" ),
                _ => $"""
                         Invalid span type '{spanSpec}'. Allowed are "braces", "brackets".
                         """
            };
        }
        return ITokenFilterOperator.Empty;
    }

    protected override void ParseStandardMatchPattern( ref TokenizerHead head )
    {
        _reusableScanner.Reset();
        _reusableScanner.TokenOnlyParse( ref head );
    }
}
