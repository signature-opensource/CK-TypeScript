using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Simple helper that finds the enclosed top spans in a span of tokens.
/// </summary>
public struct EnclosedSpanCoveringEnumerator
{
    readonly IReadOnlyList<Token> _tokens;
    readonly TokenSpan _span;
    readonly Func<IReadOnlyList<Token>, int, EnclosingTokenType> _enclosing;
    TokenSpan _current;
    int _iHead;

    public EnclosedSpanCoveringEnumerator( IReadOnlyList<Token> tokens,
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
        for( int i = _iHead; i < _span.End; ++i )
        {
            var t = _enclosing( _tokens, i );
            if( t is EnclosingTokenType.Open )
            {
                int depth = 0;
                int iO = i;
                while( ++i < _span.End )
                {
                    t = _enclosing( _tokens, i );
                    if( t is EnclosingTokenType.Close )
                    {
                        if( depth-- == 0 )
                        {
                            _iHead = ++i;
                            _current = new TokenSpan( iO, i );
                            return true;
                        }
                    }
                    else if( t is EnclosingTokenType.Open )
                    {
                        ++depth;
                    }
                }
            }
        }
        _iHead = _span.End;
        _current = default;
        return false;
    }
}
