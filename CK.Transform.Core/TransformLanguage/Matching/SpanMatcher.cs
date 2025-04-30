using CK.Core;
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
public sealed class SpanMatcher : SourceSpan, ITokenFilter
{
    Token? _spanType;
    Token? _languageName;
    RawString? _pattern;

    SpanMatcher( int beg, int end, Token? spanType, Token? languageName, RawString pattern )
        : base( beg, end )
    {
        _spanType = spanType;
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
    public Token? SpanType => _spanType;

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

    internal static SpanMatcher? Match( ref TokenizerHead head )
    {
        int begSpan = head.LastTokenIndex + 1;
        Token? spanType = null;
        if( head.TryAcceptToken( TokenType.OpenBrace, out _ ) )
        {
            head.TryAcceptToken( TokenType.GenericIdentifier, out spanType );
            if( !head.TryAcceptToken( TokenType.CloseBrace, out _ ) )
            {
                head.AppendMissingToken( "closing '}'" );
            }
        }
        RawString? pattern = null;
        if( head.LowLevelTokenType is not TokenType.DoubleQuote )
        {
            head.AppendMissingToken( "Pattern string" );
        }
        else
        {
            pattern = RawString.Match( ref head );
            if( pattern != null && pattern.InnerText.Length == 0 )
            {
                head.AppendError( "Pattern string must not be empty.", -1 );
                pattern = null;
            }
        }
        Token? languageName = null;
        if( head.TryAcceptToken( TokenType.Dot, out _ ) )
        {
            if( !head.TryAcceptToken( TokenType.GenericIdentifier, out languageName ) )
            {
                head.AppendMissingToken( "language name" );
            }
        }
        return pattern == null
                ? null
                : head.AddSpan( new SpanMatcher( begSpan,
                                                 head.LastTokenIndex + 1,
                                                 spanType,
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
                return builder.EmptyResult;
            }
        }
        var m = language.TargetAnalyzer.CreateSpanMatcher( builder.Monitor,
                                                           _spanType != null ? _spanType.Text.Span : default,
                                                           _pattern.InnerText );
        return m != null
                ? m.GetScopedTokens( builder )
                : builder.EmptyResult;
    }

    public override string ToString()
    {
        if( !CheckValid() ) return "<Invalid>";
        return _spanType == null
                ? _pattern.ToString()
                : $$"""{{{_spanType.Text.Span}}} {{_pattern?.Text}}""";
    }
}
