using CK.Core;
using System;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// A transform language analyzer is a <see cref="TargetLanguageAnalyzer"/>.
/// <para>
/// It handles the top-level <c></c>create &lt;language&gt; transformer [name] [on &lt;target&gt;] [as] begin ... end'.
/// Actual statements analysis is delegated to the virtual <see cref="ParseStatement"/> of the found <c>&lt;language&gt;</c>'s
/// transform language analyzer: any transform language analyzer can actually handle any available language.
/// </para>
/// </summary>
public class TransformLanguageAnalyzer : TargetLanguageAnalyzer, ITopLevelAnalyzer<TransformerFunction>
{
    readonly TransformerHost.Language _language;
    readonly TargetLanguageAnalyzer _targetAnalyzer;

    /// <summary>
    /// Initializes a new transform analyzer.
    /// </summary>
    /// <param name="language">The transform language.</param>
    /// <param name="targetAnalyzer">The target analyzer.</param>
    internal protected TransformLanguageAnalyzer( TransformerHost.Language language, TargetLanguageAnalyzer targetAnalyzer )
        : base( language.LanguageName + ".T" )
    { 
        _language = language;
        _targetAnalyzer = targetAnalyzer;
    }

    /// <summary>
    /// Internal constructor for the root transformer of transformer.
    /// </summary>
    /// <param name="rootLanguage"></param>
    internal TransformLanguageAnalyzer( TransformerHost.Language rootLanguage )
        : base( TransformerHost._transformLanguageName )
    {
        Throw.DebugAssert( rootLanguage.TransformLanguage.IsAutoLanguage );
        _language = rootLanguage;
        _targetAnalyzer = this;
    }

    /// <summary>
    /// Gets the <see cref="TransformerHost.Language"/>.
    /// </summary>
    public TransformerHost.Language Language => _language;

    /// <summary>
    /// Gets the target analyzer.
    /// </summary>
    public TargetLanguageAnalyzer TargetAnalyzer => _targetAnalyzer;

    /// <summary>
    /// Transform languages accept <see cref="TriviaHeadExtensions.AcceptCLikeRecursiveStarComment(ref TriviaHead)"/>
    /// and <see cref="TriviaHeadExtensions.AcceptCLikeLineComment(ref TriviaHead)"/>.
    /// <para>
    /// This cannot be changed.
    /// </para>
    /// </summary>
    /// <param name="c">The trivia head.</param>
    protected sealed override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeRecursiveStarComment();
        c.AcceptCLikeLineComment();
    }

    /// <summary>
    /// Supports the minimal token set required by any transform language:
    /// <list type="bullet">
    ///     <item><see cref="TokenType.GenericIdentifier"/> that at least handles "Ascii letter[Ascii letter or digit]*".</item>
    ///     <item><see cref="TokenType.GenericNumber"/> that at least handles "Ascii digit[Ascii digit]*".</item>
    ///     <item><see cref="TokenType.DoubleQuote"/>.</item>
    ///     <item><see cref="TokenType.LessThan"/>.</item>
    ///     <item><see cref="TokenType.Dot"/>.</item>
    ///     <item><see cref="TokenType.SemiColon"/>.</item>
    ///     <item><see cref="TokenType.Plus"/>.</item>
    ///     <item><see cref="TokenType.Minus"/>.</item>
    ///     <item><see cref="TokenType.OpenBrace"/>.</item>
    ///     <item><see cref="TokenType.CloseBrace"/>.</item>
    ///     <item><see cref="TokenType.Asterisk"/>.</item>
    /// </list>
    /// <para>
    /// This can be overridden to support other low level token types.
    /// </para>
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The low level token.</returns>
    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head )
    {
        var c = head[0];
        if( char.IsAsciiLetter( c ) )
        {
            int iS = 0;
            while( ++iS < head.Length && char.IsAsciiLetterOrDigit( head[iS] ) ) ;
            return new LowLevelToken( TokenType.GenericIdentifier, iS );
        }
        if( char.IsAsciiDigit( c ) )
        {
            int iS = 0;
            while( ++iS < head.Length && char.IsAsciiDigit( head[iS] ) ) ;
            return new LowLevelToken( TokenType.GenericNumber, iS );
        }
        return c switch
        {
            '"' => new LowLevelToken( TokenType.DoubleQuote, 1 ),
            '.' => new LowLevelToken( TokenType.Dot, 1 ),
            ';' => new LowLevelToken( TokenType.SemiColon, 1 ),
            '<' => new LowLevelToken( TokenType.LessThan, 1 ),
            '+' => new LowLevelToken( TokenType.Plus, 1 ),
            '-' => new LowLevelToken( TokenType.Minus, 1 ),
            '{' => new LowLevelToken( TokenType.OpenBrace, 1 ),
            '}' => new LowLevelToken( TokenType.CloseBrace, 1 ),
            '*' => new LowLevelToken( TokenType.Asterisk, 1 ),
            _ => default
        };
    }

    /// <summary>
    /// Handles the top-level 'create &lt;language&gt; transformer [name] [on &lt;target&gt;] [as] begin ... end'
    /// and is used by the <see cref="TransformerHost"/>. This cannot be overridden.
    /// <para>
    /// This doesn't forward the head and doesn't add errors if the text doesn't start with a <c>create</c> token.
    /// </para>
    /// </summary>
    /// <param name="head">The head.</param>
    protected sealed override void DoParse( ref TokenizerHead head )
    {
        int begText = head.RemainingTextIndex;
        if( !head.TryAcceptToken( "create", out _ ) )
        {
            return;
        }
        int startFunction = head.LastTokenIndex;

        var targetLanguage = InjectionPoint.TryMatch( ref head );
        if( targetLanguage == null )
        {
            head.AppendError( $"Expected target <language>.", 0 );
            return;
        }
        var cLang = _language.Host.FindLanguage( targetLanguage.Name, withFileExtensions: true );
        if( cLang == null )
        {
            head.AppendError( $"Target language '{targetLanguage.Name}' not found. Available languages are: '{_language.Host.Languages.Select( l => l.LanguageName ).Concatenate( "', '" )}'.", 0 );
            return;
        }

        head.MatchToken( "transformer" );

        Token? functionName = null;
        string? target = null;
        if( !head.LowLevelTokenText.Equals( "begin", StringComparison.Ordinal ) )
        {
            bool hasOn = head.LowLevelTokenText.Equals( "on", StringComparison.Ordinal );
            if( !hasOn && !head.LowLevelTokenText.Equals( "as", StringComparison.Ordinal ) )
            {
                functionName = head.AcceptLowLevelToken();
                hasOn = head.LowLevelTokenText.Equals( "on", StringComparison.Ordinal );
            }
            if( hasOn )
            {
                // Eats "on".
                head.AcceptLowLevelToken();
                // Either an identifier or a single-line string.
                if( head.LowLevelTokenType == TokenType.GenericIdentifier )
                {
                    target = head.AcceptLowLevelToken().ToString();
                }
                else if( head.LowLevelTokenType == TokenType.DoubleQuote )
                {
                    target = RawString.Match( ref head, maxLineCount: 1 )?.Lines[0];
                }
                else
                {
                    if( head.LowLevelTokenText.Equals( "as", StringComparison.Ordinal )
                        || head.LowLevelTokenText.Equals( "begin", StringComparison.Ordinal ) )
                    {
                        head.AppendMissingToken( "target (identifier or one-line string)" );
                    }
                    else
                    {
                        head.AppendUnexpectedToken();
                    }
                }
            }
            // The optional "as" token is parsed in the context of the root transform language.
            head.TryAcceptToken( "as", out _ );
        }
        // The begin...end is parsed in the context of the transformed language.
        var headStatements = head.CreateSubHead( out var safetyToken, cLang.TransformLanguageAnalyzer );
        var statements = TransformStatementBlock.Parse( cLang.TransformLanguageAnalyzer, ref headStatements );
        head.SkipTo( safetyToken, ref headStatements );
        var functionText = head.Text.Slice( begText, head.RemainingTextIndex );
        head.AddSpan( new TransformerFunction( functionText,
                                               startFunction,
                                               head.LastTokenIndex + 1,
                                               cLang,
                                               statements,
                                               functionName?.ToString(),
                                               target ) );
    }

    /// <summary>
    /// Must implement transform specific statement parsing.
    /// <para>
    /// At this level, this handles transform statements that apply to any language:
    /// <see cref="ReparseStatement"/>, <see cref="InjectIntoStatement"/>,
    /// <see cref="InScopeStatement"/>, <see cref="ReplaceStatement"/> and
    /// <see cref="TransformStatementBlock"/> (<c>begin</c>...<c>end</c> blocks).
    /// </para>
    /// </summary>
    /// <param name="head">The head.</param>
    /// <returns>The parsed statement or null.</returns>
    internal protected virtual TransformStatement? ParseStatement( ref TokenizerHead head )
    {
        if( head.TryAcceptToken( "inject", out var inject ) )
        {
            return InjectIntoStatement.Parse( ref head, inject );
        }
        if( head.TryAcceptToken( "in", out var inT ) )
        {
            return InScopeStatement.Parse( this, ref head, inT );
        }
        if( head.TryAcceptToken( "replace", out var replaceT ) )
        {
            return ReplaceStatement.Parse( this, ref head, replaceT );
        }
        if( head.TryAcceptToken( "reparse", out _ ) )
        {
            int begStatement = head.LastTokenIndex;
            head.TryAcceptToken( TokenType.SemiColon, out _ );
            return head.AddSpan( new ReparseStatement( begStatement, head.LastTokenIndex + 1 ) );
        }
        if( head.LowLevelTokenText.Equals( "begin", StringComparison.Ordinal ) )
        {
            return TransformStatementBlock.Parse( this, ref head );
        }
        return null;
    }

    /// <summary>
    /// Overridden to handle "{statement}", "{inject}", "{replace}" and return a <see cref="SingleSpanTypeOperator"/> on success.
    /// </summary>
    /// <param name="tokenSpec">The span specification to analyze.</param>
    /// <returns>The provider or an error string.</returns>
    internal protected override object ParseSpanSpec( RawString tokenSpec )
    {
        var singleSpanType = tokenSpec.InnerText.Span.Trim();
        if( singleSpanType.Length > 0 )
        {
            return singleSpanType switch
            {
                "statement" => new SingleSpanTypeOperator( typeof( TransformStatement ), "{statement}" ),
                "inject" => new SingleSpanTypeOperator( typeof( InjectIntoStatement ), "{inject}" ),
                "replace" => new SingleSpanTypeOperator( typeof( ReplaceStatement ), "{replace}" ),
                _ => $"""
                         Invalid span type '{singleSpanType}'. Allowed are "statement", "inject", "replace".
                         """
            };
        }
        return ITokenFilterOperator.Empty;
    }

    protected override void ParseStandardMatchPattern( ref TokenizerHead head )
    {
        while( head.EndOfInput == null )
        {
            if( head.LowLevelTokenType is TokenType.DoubleQuote )
            {
                RawString.Match( ref head );
            }
            else if( head.LowLevelTokenType is TokenType.OpenBrace )
            {
                RawString.MatchAnyQuote( ref head, '{', '}' );
            }
            else
            {
                head.AcceptLowLevelTokenOrNone();
            }
        }
    }
}
