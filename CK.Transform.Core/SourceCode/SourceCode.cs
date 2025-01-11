using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Source code is created by a <see cref="TokenizerHead"/> and can be mutated by a <see cref="SourceCodeEditor"/>.
/// </summary>
public sealed class SourceCode : IEnumerable<SourceToken>
{
    internal readonly SourceSpanRoot _spans;
    ImmutableList<Token> _tokens;
    string? _toString;

    internal SourceCode( List<Token> tokens, SourceSpanRoot spans, string? sourceText )
    {
        _spans = new SourceSpanRoot();
        if( spans._children._firstChild != null ) spans.TransferTo( _spans );
        _tokens = ImmutableList.CreateRange( tokens );
        tokens.Clear();
        _toString = sourceText;
    }

    /// <summary>
    /// Gets the spans.
    /// </summary>
    public ISourceSpanRoot Spans => _spans;

    /// <summary>
    /// Gets the tokens.
    /// </summary>
    public ImmutableList<Token> Tokens => _tokens;

    /// <summary>
    /// Enumerates all <see cref="Tokens"/> with their index and deepest span in an optimized way.
    /// <para>
    /// Note that the enumerator MUST be disposed once done with it because it contains a <see cref="ImmutableList{T}.Enumerator"/>
    /// that must be disposed.
    /// </para>
    /// </summary>
    public IEnumerable<SourceToken> SourceTokens => this;

    IEnumerator<SourceToken> IEnumerable<SourceToken>.GetEnumerator() => new SourceTokenEnumerator( this );

    IEnumerator IEnumerable.GetEnumerator() => new SourceTokenEnumerator( this );

    internal void SetTokens( ImmutableList<Token> tokens )
    {
        _tokens = tokens;
        _toString = null;
    }

    internal void TransferTo( SourceCode code )
    {
        code.SetTokens( _tokens );
        code._spans._children.Clear();
        if( _spans._children.HasChildren ) _spans.TransferTo( code._spans );
    }

    sealed class SourceTokenEnumerator : IEnumerator<SourceToken>
    {
        ImmutableList<Token>.Enumerator _tokenEnumerator;
        Token? _token;
        SourceSpan? _nextSpan;
        SourceSpan? _span;
        int _index;
        readonly SourceCode _code;

        public SourceTokenEnumerator( SourceCode code )
        {
            _code = code;
            _index = -1;
            _nextSpan = code._spans._children._firstChild;
            _tokenEnumerator = code._tokens.GetEnumerator();
        }

        public SourceToken Current => new SourceToken( _token!, _span, _index );

        object IEnumerator.Current => Current;

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

        public void Reset()
        {
            _tokenEnumerator.Reset();
            _index = -1;
            _nextSpan = _code._spans._children._firstChild;
        }
    }

    /// <summary>
    /// Overridden to return the text of the source.
    /// </summary>
    /// <returns>The text.</returns>
    public override string ToString() => _toString ??= _tokens.Write( new StringBuilder() ).ToString();
}
