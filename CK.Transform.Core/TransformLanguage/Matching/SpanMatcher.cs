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
/// {span specification} "pattern"
/// </para>
/// <list type="bullet">
///     <item>At least the span specification or the pattern string is required.</item>
///     <item>When the span specification not specified, it defaults to the matched tokens.</item>
/// </list>
/// </summary>
public sealed partial class SpanMatcher : SourceSpan
{
    readonly ITokenFilterOperator? _spanSpec;
    readonly ITokenFilterOperator? _pattern;
    
    SpanMatcher( int beg, int end,
                 ITokenFilterOperator? spanSpec,
                 ITokenFilterOperator? pattern )
        : base( beg, end )
    {
        _spanSpec = spanSpec;
        _pattern = pattern;
    }

    internal static SpanMatcher? Match( LanguageTransformAnalyzer analyzer, ref TokenizerHead head )
    {
        int begSpan = head.LastTokenIndex + 1;

        // Try to match both, even if an error occurred on the specification.
        bool success = TryMatchSpec( analyzer, ref head, out var specProvider );
        success &= TryMatchPattern( analyzer, ref head, specProvider, out var patternProvider );
        if( !success ) return null;

        if( specProvider == null && patternProvider == null )
        {
            head.AppendError( "Missing {span specification} and/or \"pattern\".", 0 );
            return null;
        }
        return head.AddSpan( new SpanMatcher( begSpan,
                                              head.LastTokenIndex + 1,
                                              specProvider,
                                              patternProvider ) );

        static bool TryMatchSpec( LanguageTransformAnalyzer analyzer,
                                  ref TokenizerHead head,
                                  out ITokenFilterOperator? specProvider )
        {
            specProvider = null;
            if( head.LowLevelTokenType is TokenType.OpenBrace )
            {
                var tokenSpec = RawString.MatchAnyQuote( ref head, '{', '}' );
                if( tokenSpec == null )
                {
                    return false;
                }
                object m = analyzer.TargetAnalyzer.ParseSpanSpec( tokenSpec );
                if( m is not string and not ITokenFilterOperator )
                {
                    Throw.InvalidOperationException( $"{analyzer.TargetAnalyzer.GetType().FullName}.ParseSpanSpec() must return a string or a IFilteredTokenEnumerableProvider." );
                }
                if( m is string error )
                {
                    head.AppendError( error, -1 );
                    return false;
                }
                specProvider = Unsafe.As<ITokenFilterOperator>( m );
            }
            return true;
        }

        static bool TryMatchPattern( LanguageTransformAnalyzer analyzer,
                                     ref TokenizerHead head,
                                     ITokenFilterOperator? specProvider,
                                     out ITokenFilterOperator? patternProvider )
        {
            patternProvider = null;
            if( head.LowLevelTokenType is TokenType.DoubleQuote )
            {
                var tokenPattern = RawString.Match( ref head );
                if( tokenPattern == null )
                {
                    return false;
                }
                object m = analyzer.TargetAnalyzer.ParsePattern( tokenPattern, specProvider );
                if( m is not string and not ITokenFilterOperator )
                {
                    Throw.InvalidOperationException( $"{analyzer.TargetAnalyzer.GetType().FullName}.ParsePattern() must return a string or a IFilteredTokenEnumerableProvider." );
                }
                if( m is string error )
                {
                    head.AppendError( error, -1 );
                    return false;
                }
                patternProvider = Unsafe.As<ITokenFilterOperator>( m );
            }
            return true;
        }
    }


    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "SpanMatcher[ " );
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
