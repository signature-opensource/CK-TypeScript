using CK.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="ITokenFilterOperator"/> that finds the deepest
/// spans defined by opening/closing pair of tokens.
/// This is like the <see cref="SpanTypeOperator"/> but "dynamic" as no
/// <see cref="SourceSpan"/> are required to exist.
/// </summary>
public sealed class SpanEnclosedOperator : ITokenFilterOperator
{
    readonly string _openingToken;
    readonly string _closingToken;

    public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

    public SpanEnclosedOperator( string openingToken, string closingToken )
    {
        _openingToken = openingToken;
        _closingToken = closingToken;
    }

    public void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        var builder = context.SharedBuilder;
        var spanCollector = new TokenSpanDeepestCollector();
        var e = input.CreateTokenEnumerator();
        while( e.NextEach() )
        {
            while( e.NextMatch() )
            {
                while( e.NextToken() )
                {

                }
            }
        }
        context.SetResult( builder );

    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {

        throw new NotImplementedException();
    }

    public override string ToString() => Describe( new StringBuilder(), true ).ToString();

    abstract class DeepestEnclosedSpanEnumerator
    {
        readonly IReadOnlyList<Token> _tokens;
        readonly TokenSpan _anchor;
        readonly TokenSpan _scope;
        int _iO;
        int _iC;

        protected enum TType { None, Open, Close }

        protected DeepestEnclosedSpanEnumerator( IReadOnlyList<Token> tokens, TokenSpan anchor, TokenSpan scope )
        {
            _tokens = tokens;
            _anchor = anchor;
            _scope = scope;

            // Initializing the 2 indexes inside the anchor to:
            // - the first '...}...'
            // - or first deepest '...{...}...'
            // - or last '...{...'
            // - or nothing.
            //
            // We can endup with (iO,iC):
            // (-1,>=0) => "Leading".
            //             Span candidate that must start on or after
            //             scope.Beg, providing that there is no '}' between scope.Beg and anchor.Beg.
            // (>=0, iC > iO) => "Regular case". Regular first span to return.
            // (>=0,-1) => "Single trailing",
            //             Single span candidate that must end on or after
            //             scope.End, providing that there is no '{' between anchor.End and scope.End.
            // (-1,-1) => None.
            //
            int iO = -1;
            int iC = -1;
            for( int i = anchor.Beg; i < anchor.End; ++i )
            {
                var t = GetTokenType( _tokens[i], i );
                if( t is TType.Open )
                {
                    iO = i;
                }
                else if( t is TType.Close )
                {
                    iC = i;
                    break;
                }
            }
            _iO = iO;
            _iC = iC;
        }

        public TokenSpan GetNext()
        {
            var result = TokenSpan.Empty;
            if( _iO >= 0 )
            {
                if( _iC >= 0 )
                {
                    // Regular case.
                    result = new TokenSpan( _iO, _iC );
                    Forward();
                }
                else
                {
                    // Single trailing case.
                }
            }
            else if( _iC >= 0 )
            {
                // Leading case.
                // We try to find an opening '{' in the scope without inner closing '}'.
                int iOScoped = _anchor.Beg - 1;
                while( iOScoped >= _scope.Beg )
                {
                    var t = GetTokenType( _tokens[iOScoped], iOScoped );
                    if( t is TType.Open )
                    {
                        break;
                    }
                    if( t is TType.Close )
                    {
                        iOScoped = -1;
                        break;
                    }
                    --iOScoped;
                }
                if( iOScoped >= _scope.Beg )
                {
                    // Found a leading span that starts in the scope and ends in the anchor.
                    result = new TokenSpan( iOScoped, _iC );
                    _iC = -1;
                }

            }


            if( _iO >= 0 )
            {
                if( _iC >= 0 )
                {
                    if( _iO < _iC )
                    {
                        // Regular case.
                    }
                    else
                    {
                        // Initial edge case: a closing '}' appears before the first opening '{'.
                    }
                }
                else
                {
                    // Opening, no Closing.
                    // The single last possible range is after anchor.
                    for( int i = _anchor.End; i < _scope.End; i++ )
                    {
                        if( IsClosing( _tokens[i] ) )
                        {
                            result = new TokenSpan( _iO, i + 1 );
                        }
                    }
                    _iO = -1;
                }
            }
            else if( _iC >= 0 )
            {
                // No Opening, Closing.
                // The single last possible range is before anchor.
                for( int i = _anchor.Beg - 1; i >= _scope.Beg; i-- )
                {
                    if( IsOpening( _tokens[i] ) )
                    {
                        return new TokenSpan( i, _iC + 1 );
                    }
                }
                _iC = -1;
            }
            return result;
        }

        private void Forward()
        {
            // Forward to next opening '{', ignoring any closing '}'
            _iO = ++_iC;
            while( _iO < _anchor.End )
            {
                if( IsOpening( _tokens[_iO] ) )
                {
                    break;
                }
                ++_iO;
            }
            _iC = _iO + 1;
            // Finding the next closing '}' up to the end of the scope,
            // resetting the opening '{' when we find one, even after the
            // end of the anchor: eventually, if the iC is on or after anchor.End,
            // we are done.
            while( _iC < _scope.End )
            {
                // Challeng the closing first and use a else clause
                // to allow IsOpening/Closing to return true for the
                // same token.
                if( IsClosing( _tokens[_iC] ) )
                {
                    break;
                }
                else if( IsOpening( _tokens[_iC] ) )
                {
                    // Early exit if we know we failed.
                    if( (_iO = _iC) >= _anchor.End )
                    {
                        break;
                    }
                }
                ++_iC;
            }
            // Either we found the 2 indexes or we are done.
            if( _iO >= _anchor.End || _iC >= _scope.End )
            {
                _iO = -1;
                _iC = -1;
            }
        }

        protected IReadOnlyList<Token> Tokens => _tokens;

        protected abstract TType GetTokenType( Token token, int index );

        protected abstract bool IsOpening( Token t );

        protected abstract bool IsClosing( Token t );
    }
}
