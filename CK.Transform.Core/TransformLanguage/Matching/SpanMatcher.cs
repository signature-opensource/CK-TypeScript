using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Captures a span matcher:
/// <para>
/// {<see cref="SpanType"/>} .<see cref="LanguageName"/><see cref="Pattern"/>
/// </para>
/// Where the span type is optional (defaults to the matched tokens) and the ".<see cref="LanguageName"/>"
/// suffix is also optional (defaults to the target language).
/// </summary>
public sealed class SpanMatcher : SourceSpan, ITokenFilter, IFilteredTokenEnumerableProvider
{
    Token? _spanSpec;
    Token? _languageName;
    RawString? _pattern;

    SpanMatcher( int beg, int end, Token? spanType, Token? languageName, RawString? pattern )
        : base( beg, end )
    {
        _spanSpec = spanType;
        _languageName = languageName;
        _pattern = pattern;
    }

    [MemberNotNullWhen( true, nameof( Pattern ), nameof( _pattern ) )]
    public override bool CheckValid()
    {
        return base.CheckValid() && _pattern != null;
    }

    /// <summary>
    /// Gets the type of the span. This is a <see cref="BasicTokenType.GenericIdentifier"/>.
    /// </summary>
    public Token? SpanType => _spanSpec;

    /// <summary>
    /// Gets or sets the optional language name. This is a <see cref="BasicTokenType.GenericIdentifier"/>.
    /// When null (the default), the target language is used.
    /// </summary>
    public Token? LanguageName => _languageName;

    /// <summary>
    /// Gets the pattern to match.
    /// The <see cref="RawString.InnerText"/> must not be empty.
    /// </summary>
    public RawString? Pattern => _pattern;

    internal static SpanMatcher? Match( TransformerHost.Language language, ref TokenizerHead head )
    {
        int begSpan = head.LastTokenIndex + 1;
        Token? spanSpec = null;
        if( head.LowLevelTokenType == TokenType.OpenBrace )
        {
            var basicString = LowLevelToken.BasicallyReadQuotedString( head.LowLevelTokenText );
            Throw.DebugAssert( basicString.TokenType is TokenType.GenericString or TokenType.ErrorUnterminatedString );
            if( basicString.TokenType == TokenType.ErrorUnterminatedString )
            {
                head.AppendError( "Missing closing '}'.", 1, TokenType.GenericUnexpectedToken|TokenType.ErrorClassBit );
            }
            else
            {
                spanSpec = head.AcceptLowLevelToken();
            }
        }
        RawString? pattern = null;
        if( head.LowLevelTokenType is not TokenType.DoubleQuote )
        {
            // This should not be done here but in Bind!
            if( spanSpec == null ) head.AppendMissingToken( "span {specification} and/or pattern \"string\"" );
        }
        else
        {
            pattern = RawString.Match( ref head );
            if( pattern != null && pattern.InnerText.Span.Trim().Length == 0 )
            {
                head.AppendError( "Pattern string must not be empty.", -1 );
                pattern = null;
            }
        }
        TransformerHost.Language? patternLanguage = language;
        if( head.TryAcceptToken( TokenType.Dot, out _ ) )
        {
            if( !head.TryAcceptToken( TokenType.GenericIdentifier, out var languageName ) )
            {
                head.AppendMissingToken( "language name" );
            }
            else
            {
                patternLanguage = language.Host.FindLanguage( languageName.Text.Span );
                if( patternLanguage == null )
                {
                    head.AppendError( $"Unknwon language.", -1 );
                }
            }
        }
        return patternLanguage == null || (spanSpec == null && pattern == null)
                ? null
                : head.AddSpan( new SpanMatcher( begSpan,
                                                 head.LastTokenIndex + 1,
                                                 spanSpec,
                                                 languageName,
                                                 pattern ) );
    }

    public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> GetScopedTokens( ScopedTokensBuilder builder )
    {
        Throw.DebugAssert( CheckValid() );
        var language = builder.Language;
        // This should be in Bind() method.
        // => Currently we don't locate the error (we don't have the source code of the transformer here).
        if( _languageName != null )
        {
            language = builder.Language.Host.FindLanguage( _languageName.Text.Span );
            if( language == null )
            {
                builder.Monitor.Error( $"Unable to find language '{_languageName}'." );
                return ScopedTokensBuilder.EmptyResult;
            }
        }
        var m = language.TargetAnalyzer.CreateSpanMatcher( builder.Monitor,
                                                           _spanSpec != null ? _spanSpec.Text.Span : default,
                                                           _pattern.InnerText );
        return m != null
                ? m.GetScopedTokens( builder )
                : ScopedTokensBuilder.EmptyResult;
    }

    public override string ToString()
    {
        if( !CheckValid() ) return "<Invalid>";
        return _spanSpec == null
                ? _pattern.ToString()
                : $"""{_spanSpec.Text.Span} {_pattern?.Text}""";
    }

    public Func<IActivityMonitor, IEnumerable<IEnumerable<IEnumerable<SourceToken>>>, IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection()
    {
        throw new NotImplementedException();
    }
}
