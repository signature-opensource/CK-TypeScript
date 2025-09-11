using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Simple helper that finds the deepest enclosed spans in a span of tokens.
/// </summary>
public struct EnclosedSpanDeepestEnumerator
{
    readonly IReadOnlyList<Token> _tokens;
    readonly TokenSpan _span;
    readonly Func<IReadOnlyList<Token>,int, EnclosingTokenType> _enclosing;
    TokenSpan _current;
    int _iHead;

    public EnclosedSpanDeepestEnumerator( IReadOnlyList<Token> tokens,
                                          TokenSpan span,
                                          Func<IReadOnlyList<Token>, int, EnclosingTokenType> enclosing )
    {
        _tokens = tokens;
        _span = span;
        _enclosing = enclosing;
        _iHead = span.Beg;
    }

    public TokenSpan Current => _current;

    public bool MoveNext()
    {
        int iO = -1;
        for( int i = _iHead; i < _span.End; ++i )
        {
            var t = _enclosing( _tokens, i );
            if( t is EnclosingTokenType.Open )
            {
                iO = i;
            }
            else if( t is EnclosingTokenType.Close )
            {
                if( iO >= 0 )
                {
                    _iHead = ++i;
                    _current = new TokenSpan( iO, _iHead );
                    return true;
                }
            }
        }
        _current = default;
        return false;
    }
}
