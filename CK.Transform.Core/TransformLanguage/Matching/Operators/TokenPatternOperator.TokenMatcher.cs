using System;
using System.Collections.Immutable;

namespace CK.Transform.Core;

public sealed partial class TokenPatternOperator
{
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

        internal void FilterWhere( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
        {
            var builder = context.SharedBuilder;
            var e = input.CreateTokenEnumerator();
            while( e.NextEach( skipEmpty: false ) )
            {
                while( e.NextMatch() )
                {
                    Reset();
                    while( e.NextToken() )
                    {
                        var s = Match( e.Token );
                        if( !s.IsEmpty )
                        {
                            builder.AddMatch( e.CurrentMatch.Span );
                            break;
                        }
                    }
                }
                builder.StartNewEach( skipEmpty: false );
            }
            context.SetResult( builder );
        }

        internal void CreateMatches( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
        {
            var builder = context.SharedBuilder;
            var e = input.CreateTokenEnumerator();
            while( e.NextEach( skipEmpty: false ) )
            {
                while( e.NextMatch() )
                {
                    Reset();
                    while( e.NextToken() )
                    {
                        var s = Match( e.Token );
                        if( !s.IsEmpty ) builder.AddMatch( s );
                    }
                }
                builder.StartNewEach( skipEmpty: false );
            }
            context.SetResult( builder );
        }

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

    }

}
