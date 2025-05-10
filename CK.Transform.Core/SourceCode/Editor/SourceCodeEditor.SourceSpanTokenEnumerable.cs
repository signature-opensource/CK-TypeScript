using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    internal sealed class SourceSpanTokenEnumerable : IEnumerable<SourceToken>, IEnumerator<SourceToken>
    {
        readonly SourceCodeEditor _editor;
        readonly SourceSpan _span;
        List<Token>? _enumTokens;
        Token? _token;
        int _index;

        public SourceSpanTokenEnumerable( SourceCodeEditor editor, SourceSpan span )
        {
            _editor = editor;
            _span = span;
        }

        [MemberNotNullWhen( false, nameof( _enumTokens ) )]
        bool IsDisposed => _enumTokens == null;

        public void Dispose()
        {
            // We use this as the empty enumerator (when the span is detached).
            if( _enumTokens != null )
            {
                _enumTokens = null;
                _editor._enumerators.Remove( this );
            }
        }

        public SourceToken Current => new SourceToken( _token!, _index );

        public IEnumerator<SourceToken> GetEnumerator()
        {
            Throw.CheckState( "Multiple use of the UnfilteredSourceTokens.", IsDisposed );
            if( _span.IsDetached )
            {
                // MoveNext wil always return false.
                // We don't enslist this empty enumerator.
                _index = _span.Span.End;
            }
            else
            {
                _enumTokens = _editor._tokens;
                _index = _span.Span.Beg - 1;
                _editor._enumerators.Add( this );
            }
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool MoveNext()
        {
            Throw.CheckState( !IsDisposed );
            if( ++_index >= _span.Span.End ) return false;
            _token = _enumTokens[_index];
            return true;
        }

        internal void OnInsertTokens( int eLimit, int delta )
        {
            if( eLimit > _index ) ThrowUnobserved( eLimit );
            _index += delta;
        }

        internal void OnRemoveTokens( int eLimit, int count )
        {
            if( eLimit > _index ) ThrowUnobserved( eLimit );
            _index -= count;
        }

        void ThrowUnobserved( int eLimit )
        {
            Throw.CKException( $"Enumerable on '{_span}' at {eLimit} has not been observed (current is {_index})." );
        }

        void IEnumerator.Reset() => Throw.NotSupportedException();

        object IEnumerator.Current => Current;

    }


}
