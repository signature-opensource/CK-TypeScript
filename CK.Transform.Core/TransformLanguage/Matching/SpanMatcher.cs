using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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
/// </summary>
public sealed class SpanMatcher : SourceSpan, ITokenFilter, IFilteredTokenEnumerableProvider
{
    readonly Token? _languageName;
    readonly RawString? _spanSpec;
    readonly RawString? _pattern;
    readonly IFilteredTokenEnumerableProvider _provider;

    SpanMatcher( int beg, int end,
                 Token? languageName,
                 RawString? spanSpec,
                 RawString? pattern,
                 IFilteredTokenEnumerableProvider provider )
        : base( beg, end )
    {
        _languageName = languageName;
        _spanSpec = spanSpec;
        _pattern = pattern;
        _provider = provider;
    }

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
        Token? languageName = null;
        TransformerHost.Language? matcherLanguage = language;
        if( head.TryAcceptToken( TokenType.Dot, out _ ) )
        {
            if( !head.TryAcceptToken( TokenType.GenericIdentifier, out languageName ) )
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
        RawString? tokenSpec = null;
        RawString? tokenPattern = null;
        if( head.LowLevelTokenType is TokenType.OpenBrace )
        {
            tokenSpec = RawString.MatchAnyQuote( ref head );
        }
        if( head.LowLevelTokenType is TokenType.DoubleQuote )
        {
            tokenPattern = RawString.MatchAnyQuote( ref head );
        }
        if( tokenSpec == null && tokenPattern == null )
        {
            head.AppendError( "Missing {span specification} and/or \"pattern\".", 0 );
        }
        // Error: invalid language or missing spec and pattern.
        if( matcherLanguage == null || (tokenSpec == null && tokenPattern == null) )
        {
            return null;
        }
        object m = matcherLanguage.TransformStatementAnalyzer.CreateFilteredTokenProvider( language, tokenSpec, tokenPattern );
        if( m is string error )
        {
            head.AppendError( error, -1 );
            return null;
        }
        return head.AddSpan( new SpanMatcher( begSpan,
                                              head.LastTokenIndex + 1,
                                              languageName,
                                              tokenSpec,
                                              tokenPattern,
                                              (IFilteredTokenEnumerableProvider)m ) );
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
        if( _languageName == null )
        {
            if( _pattern == null ) return _spanSpec!.ToString();
            if( _spanSpec == null ) return _pattern!.ToString();
        }
        return WholeString();
    }

    string WholeString()
    {
        var b = new StringBuilder();
        if( _languageName != null )
        {
            b.Append( '.' ).Append( _languageName.Text );
        }
        if( _spanSpec != null )
        {
            if( b.Length > 0 ) b.Append( ' ' );
            b.Append( _spanSpec.Text );
        }
        if( _pattern != null )
        {
            if( b.Length > 0 ) b.Append( ' ' );
            b.Append( _pattern.Text );
        }
        return b.ToString();
    }
}
