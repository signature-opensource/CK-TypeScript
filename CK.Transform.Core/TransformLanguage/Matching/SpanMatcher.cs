using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Captures a span matcher:
/// <list type="bullet">
///     <item>{span specification} alone.</item>
///     <item>"pattern" alone.</item>
///     <item>{span specification} where "pattern".</item>
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

    internal static SpanMatcher? Match( TransformLanguageAnalyzer analyzer, ref TokenizerHead head )
    {
        int begSpan = head.LastTokenIndex + 1;

        // Try to match both, even if an error occurred on the specification.
        bool success = TryMatchSpec( analyzer, ref head, out var specOperator );
        bool hasWhere = head.TryAcceptToken( "where", out _ );
        success &= TryMatchPattern( analyzer, ref head, specOperator, out var patternOperator );
        if( !success ) return null;

        if( specOperator != null )
        {
            if( hasWhere && patternOperator == null )
            {
                head.AppendError( "Expected pattern in: {span specification} where \"pattern\".", 0 );
                return null;
            }
            if( !hasWhere && patternOperator != null )
            {
                head.AppendError( "Missing 'where' before \"pattern\" in: {span specification} where \"pattern\".", 0 );
                return null;
            }
        }
        else if( hasWhere )
        {
            head.AppendError( "Missing {span specification} before 'where'.", 0 );
            return null;
        }
        if( specOperator == null && patternOperator == null )
        {
            head.AppendError( "Expected {span specification}, \"pattern\" or {span specification} where \"pattern\".", 0 );
            return null;
        }
        return head.AddSpan( new SpanMatcher( begSpan,
                                              head.LastTokenIndex + 1,
                                              specOperator,
                                              patternOperator ) );

        static bool TryMatchSpec( TransformLanguageAnalyzer analyzer,
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
                    Throw.InvalidOperationException( $"{analyzer.TargetAnalyzer.GetType().FullName}.{nameof( TargetLanguageAnalyzer.ParseSpanSpec )}() must return a string or a IFilteredTokenEnumerableProvider." );
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

        static bool TryMatchPattern( TransformLanguageAnalyzer analyzer,
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
                    Throw.InvalidOperationException( $"{analyzer.TargetAnalyzer.GetType().FullName}.{nameof( TargetLanguageAnalyzer.ParsePattern )}() must return a string or a IFilteredTokenEnumerableProvider." );
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
            if( _spanSpec != null ) b.Append( "where " );
            _pattern.Describe( b, parsable );
        }
        if( !parsable ) b.Append( " ]" );
        return b;
    }

    public override string ToString() => Describe( new StringBuilder(), parsable: true ).ToString();

}
