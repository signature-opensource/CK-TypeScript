using CK.Core;
using System;
using System.Collections.Immutable;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Implements Knuth-Morris-Pratt find algorithm.
/// </summary>
public sealed class TokenSpanFilter : ITokenFilterOperator
{
    readonly RawString _tokenPattern;
    readonly ImmutableArray<Token> _tokens;
    readonly int[] _prefixTable;

    /// <summary>
    /// Initializes a new token span filter.
    /// </summary>
    /// <param name="tokens">The tokens. Must not be empty or default.</param>
    public TokenSpanFilter( RawString tokenPattern, ImmutableArray<Token> tokens )
    {
        Throw.CheckArgument( !tokens.IsDefaultOrEmpty );
        _tokenPattern = tokenPattern;
        _tokens = tokens;
        _prefixTable = BuildPrefixTable( tokens );
    }

    static int[] BuildPrefixTable( ImmutableArray<Token> tokens )
    {
        var prefixTable = new int[tokens.Length + 1];
        prefixTable[0] = -1;
        int i = 0;
        int prefixLength = -1;
        while( i < tokens.Length )
        {
            while( prefixLength >= 0
                   && !tokens[i].Text.Span.Equals( tokens[prefixLength].Text.Span, StringComparison.Ordinal ) )
            {
                prefixLength = prefixTable[prefixLength];
            }
            prefixTable[++i] = ++prefixLength;
        }

        return prefixTable;
    }

    /// <summary>
    /// Collects this operator.
    /// </summary>
    /// <param name="collector">The operator collector.</param>
    public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

    void ITokenFilterOperator.Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        new TokenMatcher( _tokens, _prefixTable ).CreateMatches( context, input );
    }

    struct TokenMatcher
    {
        readonly ImmutableArray<Token> _pattern;
        readonly int[] _prefixTable;
        int _iMatch;

        public TokenMatcher( ImmutableArray<Token> tokens, int[] prefixTable )
        {
            _pattern = tokens;
            _prefixTable = prefixTable;
        }

        public void Reset() => _iMatch = 0;

        public int Length => _pattern.Length;

        TokenSpan Match( SourceToken t )
        {
            bool match = _pattern[_iMatch].Text.Span.Equals( t.Token.Text.Span, StringComparison.OrdinalIgnoreCase );
            while( _iMatch > 0 && !match )
            {
                _iMatch = _prefixTable[_iMatch];
                match = _pattern[_iMatch].Text.Span.Equals( t.Token.Text.Span, StringComparison.OrdinalIgnoreCase );
            }
            if( match )
            {
                _iMatch++;
            }
            if( _iMatch == Length )
            {
                int endMatch = t.Index + 1;
                var result = new TokenSpan( endMatch - Length, endMatch );
                _iMatch = 0;
                return result;
            }
            return default;
        }


        internal void CreateMatches( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
        {
            var builder = context.SharedBuilder;
            var e = input.CreateTokenEnumerator();
            while( e.NextEach() )
            {
                builder.StartNewEach();
                while( e.NextMatch() )
                {
                    Reset();
                    while( e.NextToken() )
                    {
                        var s = Match( e.Token );
                        if( !s.IsEmpty ) builder.AddMatch( s );
                    }
                    if( builder.CurrentMatchNumber == 0 )
                    {
                        MatchError( context, e );
                        return;
                    }
                }
                if( builder.CurrentMatchNumber == 0 )
                {
                    MatchError( context, e );
                    return;
                }
            }
            if( builder.CurrentMatchNumber == 0 )
            {
                MatchError( context, e );
            }
            else
            {
                context.SetResult( builder );
            }
        }

        void MatchError( ITokenFilterOperatorContext context, TokenFilterEnumerator e )
        {
            context.SetFailedResult( $"""
                                    Failed to match pattern:
                                    {_pattern.ToFullString()}
                                    """, e );
        }
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable )
        {
            b.Append( "[Pattern] \"" );
            return _tokens.WriteCompact( b ).Append( '"' );
        }
        return _tokenPattern.Lines.Length > 1
                ? b.Append( _tokenPattern.OpeningQuotes ).AppendLine()
                   .Append( _tokenPattern.TextLines ).AppendLine()
                   .Append( _tokenPattern.ClosingQuotes )
                : b.Append( _tokenPattern.Text );
    }

    public override string ToString() => Describe( new StringBuilder(), parsable: true ).ToString();

}
