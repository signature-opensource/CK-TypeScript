using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
public sealed partial class SpanMatcher : SourceSpan
{
    readonly Token? _languageName;
    readonly IFilteredTokenEnumerableProvider? _spanSpec;
    readonly IFilteredTokenEnumerableProvider? _pattern;
    
    SpanMatcher( int beg, int end,
                 Token? languageName,
                 IFilteredTokenEnumerableProvider? spanSpec,
                 IFilteredTokenEnumerableProvider? pattern )
        : base( beg, end )
    {
        _languageName = languageName;
        _spanSpec = spanSpec;
        _pattern = pattern;
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
            tokenSpec = RawString.MatchAnyQuote( ref head, '{', '}' );
        }
        if( head.LowLevelTokenType is TokenType.DoubleQuote )
        {
            tokenPattern = RawString.Match( ref head );
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
        IFilteredTokenEnumerableProvider? specProvider = null;
        if( tokenSpec != null )
        {
            object m = matcherLanguage.TransformStatementAnalyzer.ParseSpanSpec( language, tokenSpec );
            if( m is not string and not IFilteredTokenEnumerableProvider )
            {
                Throw.InvalidOperationException( $"{matcherLanguage.TransformStatementAnalyzer.GetType().FullName}.ParseSpanSpec() must return a string or a IFilteredTokenEnumerableProvider." );
            }
            if( m is string error )
            {
                head.AppendError( error, -1 );
                return null;
            }
            specProvider = Unsafe.As<IFilteredTokenEnumerableProvider>( m );
        }
        IFilteredTokenEnumerableProvider? patternProvider = null;
        if( tokenPattern != null )
        {
            object m = matcherLanguage.TransformStatementAnalyzer.ParsePattern( language, tokenPattern, specProvider );
            if( m is not string and not IFilteredTokenEnumerableProvider )
            {
                Throw.InvalidOperationException( $"{matcherLanguage.TransformStatementAnalyzer.GetType().FullName}.ParsePattern() must return a string or a IFilteredTokenEnumerableProvider." );
            }
            if( m is string error )
            {
                head.AppendError( error, -1 );
                return null;
            }
            patternProvider = Unsafe.As < IFilteredTokenEnumerableProvider>( m );
        }
        return head.AddSpan( new SpanMatcher( begSpan,
                                              head.LastTokenIndex + 1,
                                              languageName,
                                              specProvider,
                                              patternProvider ) );
    }


    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "SpanMatcher[ " );
        if( _languageName != null )
        {
            b.Append( '.' ).Append( _languageName.Text );
        }
        if( _spanSpec != null )
        {
            if( b.Length > 0 ) b.Append( ' ' );
            _spanSpec.Describe( b, parsable );
        }
        if( _pattern != null )
        {
            if( b.Length > 0 ) b.Append( ' ' );
            _pattern.Describe( b, parsable );
        }
        if( !parsable ) b.Append( " ]" );
        return b;
    }

    public override string ToString() => Describe( new StringBuilder(), parsable: true ).ToString();

}
