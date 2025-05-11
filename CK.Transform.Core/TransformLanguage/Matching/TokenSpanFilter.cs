using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Implements Knuth-Morris-Pratt find algorithm.
/// </summary>
public sealed class TokenSpanFilter : IFilteredTokenEnumerableProvider
{
    readonly ImmutableArray<Token> _tokens;
    readonly int[] _prefixTable;

    /// <summary>
    /// Initializes a new token span filter.
    /// </summary>
    /// <param name="tokens">The tokens. Must not be empty or default.</param>
    public TokenSpanFilter( ImmutableArray<Token> tokens )
    {
        Throw.CheckArgument( !tokens.IsDefaultOrEmpty );
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
    /// Collects this provider.
    /// </summary>
    /// <param name="collector">The provider collector.</param>
    public void Activate( Action<IFilteredTokenEnumerableProvider> collector ) => collector( this );

    Func<ITokenFilterBuilderContext,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> IFilteredTokenEnumerableProvider.GetFilteredTokenProjection()
    {
        return new TokenMatcher( _tokens, _prefixTable ).GetTokens;
    }

    sealed class TokenMatcher
    {
        readonly ImmutableArray<Token> _pattern;
        readonly int[] _prefixTable;
        readonly FIFOBuffer<SourceToken> _candidate;
        int _iMatch;

        public TokenMatcher( ImmutableArray<Token> tokens, int[] prefixTable )
        {
            _pattern = tokens;
            _prefixTable = prefixTable;
            _candidate = new FIFOBuffer<SourceToken>( _pattern.Length );
        }

        public void Reset() => _iMatch = 0;

        public int Length => _pattern.Length;

        public SourceToken[]? Found( SourceToken t )
        {
            bool match = _pattern[_iMatch].Text.Span.Equals( t.Token.Text.Span, StringComparison.OrdinalIgnoreCase );
            while( _iMatch > 0 && !match )
            {
                _iMatch = _prefixTable[_iMatch];
                match = _pattern[_iMatch].Text.Span.Equals( t.Token.Text.Span, StringComparison.OrdinalIgnoreCase );
            }
            if( match )
            {
                _candidate.Push( t );
                _iMatch++;
            }
            if( _iMatch == Length )
            {
                _iMatch = 0;
                return _candidate.ToArray();
            }
            return null;
        }

        public TokenSpan Match( SourceToken t )
        {
            bool match = _pattern[_iMatch].Text.Span.Equals( t.Token.Text.Span, StringComparison.OrdinalIgnoreCase );
            while( _iMatch > 0 && !match )
            {
                _iMatch = _prefixTable[_iMatch];
                match = _pattern[_iMatch].Text.Span.Equals( t.Token.Text.Span, StringComparison.OrdinalIgnoreCase );
            }
            if( match )
            {
                _candidate.Push( t );
                _iMatch++;
            }
            if( _iMatch == Length )
            {
                var result = new TokenSpan( _iMatch - Length, _iMatch );
                _iMatch = 0;
                return result;
            }
            return default;
        }

        public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> GetTokens( ITokenFilterBuilderContext c,
                                                                             IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
        {
            List<>
            foreach( var each in inner )
            {
                foreach( var range in each )
                {
                    Reset();
                    var spans = c.CreateDynamicSpan();
                    foreach( var t in range )
                    {
                        var s = Match( t );
                        if( !s.IsEmpty ) spans.AppendSpan( s );
                    }
                    if( spans.Count == 0 )
                    {
                        c.Fail( $"""
                            Unable to find pattern:
                            {_pattern.ToFullString()}
                            """ );
                    }
                        var byEach = GetRangeTokens( this, range );
                    if( byEach.Any() )
                    {
                        yield return byEach;
                    }
                }
            }

            static IEnumerable<IEnumerable<SourceToken>> GetRangeTokens( TokenMatcher matcher, IEnumerable<SourceToken> range )
            {
                matcher.Reset();
                foreach( var t in range )
                {
                    SourceToken[]? match = matcher.Found( t );
                    if( match != null )
                    {
                        yield return match;
                    }
                }
            }

        }
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "[Pattern] " );
        return _tokens.WriteCompact( b );
    }

    public override string ToString() => Describe( new StringBuilder(), parsable: true ).ToString();
}

public readonly record struct FilteredTokenSpan( int EachNumber, int RangeNumber, TokenSpan Span );

public sealed class FilteredTokenCursor
{
    ImmutableArray<FilteredTokenSpan> _spans;
    int _index;

    public bool IsValid => _index >= 0;

    public int EachNumber => _spans[_index].EachNumber;

    public int RangeNumber => _spans[_index].RangeNumber;

    public TokenSpan Span => _spans[_index].Span;

    public int Index => _index;

    public Token Token =>
}
