using CK.Core;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Source code is created by a <see cref="TokenizerHead"/> and can be mutated by a <see cref="SourceCodeEditor"/>
/// (only from a <see cref="TransformerHost.Transform(IActivityMonitor, string, IEnumerable{CK.Transform.Core.TransformerFunction})"/>).
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SourceCode : IEnumerable<SourceToken>
{
    internal readonly SourceSpanRoot _spans;
    // This can be a ImmutableList<Token> or a ImmutableList<Token>.Builder (a RB tree)
    // but ImmutableList is not that good for iteration and indexed access.
    // This should be benchmarked with real sized lists before considering this, but this is doable.
    List<Token> _tokens;
    string? _toString;

    internal SourceCode( List<Token> tokens, SourceSpanRoot spans, string? sourceText )
    {
        _spans = new SourceSpanRoot();
        if( spans._children._firstChild != null ) spans.TransferTo( _spans );
        _tokens = tokens;
        _toString = sourceText;
    }

    /// <summary>
    /// Gets the spans.
    /// </summary>
    public ISourceSpanRoot Spans => _spans;

    /// <summary>
    /// Gets the tokens.
    /// </summary>
    public IReadOnlyList<Token> Tokens => _tokens;

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

    sealed class SourceTokenEnumerator : IEnumerator<SourceToken>
    {
        List<Token>.Enumerator _tokenEnumerator;
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

        public void Reset() => Throw.NotSupportedException();
    }

    internal List<Token> InternalTokens => _tokens;

    internal void OnTokensChanged()
    {
        _toString = null;
    }

    internal void TransferTo( SourceCode code )
    {
        code._tokens = _tokens;
        code._toString = _toString;
        code._spans._children.Clear();
        if( _spans._children.HasChildren ) _spans.TransferTo( code._spans );
    }

    /// <summary>
    /// Overridden to return the text of the source.
    /// </summary>
    /// <returns>The text.</returns>
    public override string ToString() => _toString ??= _tokens.Write( new StringBuilder() ).ToString();
}
