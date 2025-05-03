using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    /// <summary>
    /// Single enumerator for root <see cref="Editor.UnfilteredTokens"/>.
    /// </summary>
    sealed class SourceTokenEnumerable : IEnumerable<SourceToken>, IEnumerator<SourceToken>
    {
        readonly SourceCodeEditor _editor;
        List<Token>? _tokens;
        Token? _token;
        SourceSpan? _nextSpan;
        SourceSpan? _span;
        int _index;

        public SourceTokenEnumerable( SourceCodeEditor editor )
        {
            _editor = editor;
        }

        [MemberNotNullWhen( false, nameof( _tokens ) )]
        bool IsDisposed => _tokens == null;

        public IEnumerator<SourceToken> GetEnumerator()
        {
            Throw.CheckState( "Multiple use of the UnfilteredSourceTokens.", IsDisposed );
            _index = -1;
            _tokens = _editor._tokens;
            _span = null;
            _nextSpan = _editor.Code._spans._children._firstChild;
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() => this;

        public void Dispose() => _tokens = null;

        public SourceToken Current => new SourceToken( _token!, _span, _index );

        public bool MoveNext()
        {
            Throw.CheckState( !IsDisposed );
            if( ++_index >= _tokens.Count ) return false;
            _token = _tokens[_index];
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

        void IEnumerator.Reset() => Throw.NotSupportedException();

        object IEnumerator.Current => Current;
    }

}
