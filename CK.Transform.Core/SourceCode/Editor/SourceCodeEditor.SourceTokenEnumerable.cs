using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    /// <summary>
    /// Tracks the enumerators to adjust them when tokens are replaced.
    /// </summary>
    sealed class SourceTokenEnumerable : IEnumerable<SourceToken>
    {
        readonly SourceCodeEditor _editor;
        Enumerator? _first;

        public SourceTokenEnumerable( SourceCodeEditor editor )
        {
            _editor = editor;
        }

        public IEnumerator<SourceToken> GetEnumerator()
        {
            var e = new Enumerator( this );
            if( _first == null )
            {
                _first = e;
            }
            else
            {
                _first._prev = e;
                e._next = _first;
                _first = e;
            }
            return e;
        }

        void Remove( Enumerator e )
        {
            if( _first == e )
            {
                if( e._next != null ) e._next._prev = null;
                _first = e._next;
            }
            else
            {
                if( _first == e )
                {
                    Throw.DebugAssert( e._prev == null );
                    if( (_first = e._next) != null )
                    {
                        _first._prev = null;
                    }
                }
                else
                {
                    Throw.DebugAssert( e._prev != null );
                    e._prev._next = e._next;
                    if( e._next != null )
                    {
                        e._next._prev = e._prev;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        sealed class Enumerator : IEnumerator<SourceToken>
        {
            SourceTokenEnumerable _e;
            readonly List<Token> _tokens;
            internal Enumerator? _next;
            internal Enumerator? _prev;

            Token? _token;
            SourceSpan? _nextSpan;
            SourceSpan? _span;
            int _index;

            public Enumerator( SourceTokenEnumerable e )
            {
                _e = e;
                _index = -1;
                _tokens = _e._editor._tokens;
                _nextSpan = e._editor.Code._spans._children._firstChild;
            }

            public void Dispose()
            {
                if( _e != null )
                {
                    _e.Remove( this );
                    _e = null!;
                }
            }
            public SourceToken Current => new SourceToken( _token!, _span, _index );

            public bool MoveNext()
            {
                Throw.CheckState( "Source code has been reparsed.", _tokens == _e._editor._tokens );
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

            public void Reset() { }

            object IEnumerator.Current => Current;
        }

    }
}
