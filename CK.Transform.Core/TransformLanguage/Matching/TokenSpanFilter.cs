using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Implements Knuth-Morris-Pratt find algorithm.
/// </summary>
sealed class TokenSpanFilter : ITokenFilter, IFilteredTokenEnumerableProvider
{
    readonly ImmutableArray<Token> _tokens;
    readonly int[] _prefixTable;

    public TokenSpanFilter( ImmutableArray<Token> tokens )
    {
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

    public Func<IActivityMonitor,
                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection()
    {
        return new TokenMatcher( _tokens, _prefixTable ).GetTokens;
    }

    public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> GetScopedTokens( ScopedTokensBuilder builder )
    {
        var matcher = new TokenMatcher( _tokens, _prefixTable );
        foreach( var each in builder.Tokens )
        {
            foreach( var range in each )
            {
                var byEach = GetRangeTokens( matcher, range );
                if( byEach.Any() )
                {
                    yield return byEach;
                }
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

        public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> GetTokens( IActivityMonitor monitor,
                                                                             IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
        {
            foreach( var each in inner )
            {
                foreach( var range in each )
                {
                    var byEach = GetRangeTokens( this, range );
                    if( byEach.Any() )
                    {
                        yield return byEach;
                    }
                }
            }

        }
    }
}
