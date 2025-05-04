using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Captures a span matcher:
/// <para>
/// .language {span specification} "pattern"
/// </para>
/// <list type="bullet">
///     <item>The .language is optional (defaults to the target language).</item>
///     <item>At least the span specification or the pattern string is required.</item>
///     <item>When the span specification not specified, it defaults to the matched tokens.</item>
/// </list>
/// This span is a wrapper around the optional language and a <see cref="SpanMatcherProvider"/> that
/// is parsed by <see cref="TransformStatementAnalyzer.CreateSpanMatcherProvider"/> of the .language.
/// </summary>
public sealed class SpanMatcher : SourceSpan, ITokenFilter, IFilteredTokenEnumerableProvider
{
    readonly TransformerHost.Language _language;
    readonly SpanMatcherProvider _provider;

    SpanMatcher( int beg, int end, TransformerHost.Language language, SpanMatcherProvider provider )
        : base( beg, end )
    {
        _language = language;
        _provider = provider;
    }

    public TransformerHost.Language Language => _language;

    public SpanMatcherProvider Provider => _provider;

    /// <summary>
    /// Relays to <see cref="Provider"/>.
    /// </summary>
    /// <returns>The fitered token projection.</returns>
    public Func<IActivityMonitor,
                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection()
    {
        return _provider.GetFilteredTokenProjection();
    }

    internal static SpanMatcher? Match( TransformerHost.Language language, ref TokenizerHead head )
    {
        int begSpan = head.LastTokenIndex + 1;
        TransformerHost.Language? matcherLanguage = language;
        if( head.TryAcceptToken( TokenType.Dot, out _ ) )
        {
            if( !head.TryAcceptToken( TokenType.GenericIdentifier, out var languageName ) )
            {
                head.AppendMissingToken( "language name" );
                matcherLanguage = null;
            }
            else
            {
                matcherLanguage = language.Host.FindLanguage( languageName.Text.Span );
                if( matcherLanguage == null )
                {
                    head.AppendError( $"Unknwon language.", -1 );
                }
            }
        }
        // Whether the language is correct or not, we create a subHead to
        // capture the {spanSpec} and the "pattern" raw strings:
        // on error, we append these tokens. On success, we append the quotes
        // of the RawString as independent generic tokens and the successfully
        // parsed tokens from the target language.
        var coverHead = head.CreateSubHead( out var safeCoverKey );
        int preTokenSpecLen = 0;
        RawString? tokenSpec = null;
        int postTokenSpecLen = 0;
        int preTokenPatternLen = 0;
        RawString? tokenPattern = null;
        int postTokenPatternLen = 0;
        if( head.LowLevelTokenType is TokenType.OpenBrace )
        {
            tokenSpec = RawString.MatchAnyQuote( ref coverHead );
            if( tokenSpec != null )
            {
                preTokenSpecLen = head.RemainingTextIndex - tokenSpec.Text.Length + tokenSpec.QuoteLength;
                postTokenSpecLen = head.Head.Length - head.RemainingTextIndex;
            }
        }
        if( head.LowLevelTokenType is TokenType.DoubleQuote )
        {
            tokenPattern = RawString.MatchAnyQuote( ref coverHead );
            if( tokenPattern != null )
            {
                preTokenPatternLen = head.RemainingTextIndex - tokenPattern.Text.Length + tokenPattern.QuoteLength;
                postTokenPatternLen = head.Head.Length - head.RemainingTextIndex;
            }
        }
        if( tokenSpec == null && tokenPattern == null )
        {
            head.AppendError( "Missing {span specification} and/or \"pattern\".", 0 );
        }
        // Error: invalid language or missing spec and pattern.
        if( matcherLanguage == null || (tokenSpec == null && tokenPattern == null) )
        {
            Throw.DebugAssert( head.FirstParseError != null );
            head.SkipTo( safeCoverKey, ref coverHead );
            return null;
        }
        // We create another subHead to honor the ILowLevelTokenizer that the TransformStatementAnalyzer may implement.
        // We don't want to use the coverHead to skip the faulty strings here because we want the errors to
        // be lifted in the primary token stream: we always skip to the matcher head.
        // It is up to the CreateSpanMatcherProvider method to correctly forwards its head.
        var matcherHead = head.CreateSubHead( out var safeMatcherKey, matcherLanguage.TransformStatementAnalyzer as ILowLevelTokenizer );
        SpanMatcherProvider? matcher = matcherLanguage.TransformStatementAnalyzer.CreateSpanMatcherProvider( ref matcherHead,
                                                                                                             preTokenSpecLen,
                                                                                                             tokenSpec,
                                                                                                             postTokenSpecLen,
                                                                                                             preTokenPatternLen,
                                                                                                             tokenPattern,
                                                                                                             postTokenPatternLen );
        if( matcher == null && matcherHead.FirstParseError == null )
        {
            Throw.CheckState( $"{matcherLanguage.LanguageName} language CreateSpanMatcherProvider must emit an error when failing to parse the {{span specification}} \"pattern\"", matcherHead.FirstParseError != null );
        }
        head.SkipTo( safeMatcherKey, ref matcherHead );
        if( matcher == null )
        {
            return null;
        }
        return head.AddSpan( new SpanMatcher( begSpan,
                                              head.LastTokenIndex + 1,
                                              matcherLanguage,
                                              matcher ) );
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

}
