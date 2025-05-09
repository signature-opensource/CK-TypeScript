using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static CK.Transform.Core.SourceCodeEditor;

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

        public bool MoveNext()
        {
            Throw.CheckState( !IsDisposed );
            if( ++_index >= _span.Span.End ) return false;
            _token = _enumTokens[_index];
            return true;
        }

        internal bool OnInsertTokens( int eLimit, int delta )
        {
            if( eLimit > _index ) Throw.CKException( $"Enumerable on '{_span}' at {eLimit} has not been observed (current is {_index})." );
            _index += delta;
            return true;
        }

        internal bool OnRemoveTokens( int eLimit, int delta )
        {
            if( eLimit > _index ) Throw.CKException( $"Dynamic enumerable at {eLimit} has not been observed (current is {_index})." );
            _index -= delta;
            return true;
        }

        void IEnumerator.Reset() => Throw.NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        object IEnumerator.Current => Current;

    }



}
