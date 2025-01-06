using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Transform.Core;


public sealed class SourceCode
{
    readonly SourceSpanRoot _spans;
    readonly TokenList _tokens;

    public SourceCode()
    {
        _spans = new SourceSpanRoot();
        _tokens = new TokenList( this );
    }

    internal SourceCode( List<Token> tokens, SourceSpanRoot spans )
    {
        _spans = new SourceSpanRoot();
        if( spans._children._firstChild != null ) spans.TransferTo( _spans );
        _tokens = new TokenList( this, tokens );
    }

    /// <summary>
    /// Gets the spans.
    /// </summary>
    public SourceSpanRoot Spans => _spans;

    /// <summary>
    /// Gets the tokens.
    /// </summary>
    public TokenList Tokens => _tokens;


    sealed class SourceCodeEnumerator : ISourceTokenEnumerator
    {
        readonly SourceCode _code;
        int _index;
        Token? _token;
        SourceSpan? _nextSpan;
        SourceSpan? _span;

        public SourceCodeEnumerator( SourceCode code )
        {
            _code = code;
            _index = -1;
            _nextSpan = code._spans._children._firstChild;
        }

        public int Index => _index;

        public Token Token => _token!;

        public SourceSpan? Span => _span;

        public bool MoveNext()
        {
            if( _index++ >= _code._tokens.Count ) return false;
            _token = _code._tokens[_index];
            if( _span == null )
            {
                if( _hasSpans && _topLevelSpan != null )
                {

                    _topLevelSpan = GetNextTopLevelSpan( _topLevelSpan, _index );
                    _span = _topLevelSpan.GetBestChildAt( _index );
                }
            }
            else 
            {
                if( _index < _span.Span.End )
                {
                    _span = _span.GetBestChildAt( _index );
                }
                else
                {
                    _span = GetNextSpan( _span );
                }
            }
            return true;

        }

        SourceSpan? GetNextSpan( SourceSpan current )
        {
            var n = current._nextSibling;
            while( n != null )
            {
                if( n.Span.Contains( _index ) )
                {
                    return n.GetBestChildAt( _index );
                }
                n = n._nextSibling;
            }
            n = current._parent;
            while( n != null )
            {
                if( n.Span.Contains( _index ) )
                {
                    return n.GetBestChildAt( _index );
                }
                n = n._parent;
            }
        }
    }

    internal void OnInsertToken( int index )
    {
        throw new NotImplementedException();
    }

    internal void OnRemoveAtToken( int index )
    {
        throw new NotImplementedException();
    }

    internal void OnRemoveRangeToken( int index, int count )
    {
        throw new NotImplementedException();
    }
}
