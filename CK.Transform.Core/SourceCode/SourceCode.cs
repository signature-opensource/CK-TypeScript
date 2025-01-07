using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace CK.Transform.Core;

public sealed class SourceCode
{
    readonly SourceSpanRoot _spans;
    ImmutableList<Token> _tokens;

    internal SourceCode( List<Token> tokens, SourceSpanRoot spans )
    {
        _spans = new SourceSpanRoot();
        if( spans._children._firstChild != null ) spans.TransferTo( _spans );
        _tokens = ImmutableList.CreateRange( tokens );
        tokens.Clear();
    }

    /// <summary>
    /// Gets the spans.
    /// </summary>
    public SourceSpanRoot Spans => _spans;

    /// <summary>
    /// Gets the tokens.
    /// </summary>
    public ImmutableList<Token> Tokens => _tokens;

    /// <summary>
    /// Creates a <see cref="ISourceTokenEnumerator"/> on all <see cref="Tokens"/>.
    /// </summary>
    /// <returns>An enumerator with index, token and spans covering the token.</returns>
    public ISourceTokenEnumerator CreateSourceTokenEnumerator() => new SourceTokenEnumerator( this );

    internal void SetTokens( ImmutableList<Token> tokens ) => _tokens = tokens;

    internal void TransferTo( SourceCode code )
    {
        code.SetTokens( _tokens );
        if( _spans._children.HasChildren ) _spans.TransferTo( code._spans );
    }

    sealed class SourceTokenEnumerator : ISourceTokenEnumerator
    {
        ImmutableList<Token>.Enumerator _tokenEnumerator;
        Token? _token;
        SourceSpan? _nextSpan;
        SourceSpan? _span;
        int _index;

        public SourceTokenEnumerator( SourceCode code )
        {
            _index = -1;
            _nextSpan = code._spans._children._firstChild;
            _tokenEnumerator = code._tokens.GetEnumerator();
        }

        public int Index => _index;

        public Token Token => _token!;

        public SourceSpan? Span => _span;

        public bool MoveNext()
        {
            if( !_tokenEnumerator.MoveNext() ) return false;
            _token = _tokenEnumerator.Current;
            ++_index;
            // If we have no span or we are leaving the current one...
            if( _span == null || _index >= _span.Span.End )
            {
                // Leaving the current span.
                _span = null;
                // If there is a next span and we are in it, we have a new span
                // and we must find a new next one.
                if( _nextSpan != null && _nextSpan.Span.Contains( _index ) )
                {
                    _span = _nextSpan.GetSpanAt( _index );
                    _nextSpan = _span._nextSibling ?? _span._parent;
                }
            }
            else 
            {
                // Still in the current span. We may enter a child.
                if( _index < _span.Span.End )
                {
                    _span = _span.GetSpanAt( _index );
                    _nextSpan = _span._nextSibling ?? _span._parent;
                }
            }
            return true;
        }

        public void Dispose() => _tokenEnumerator.Dispose();

    }
}
