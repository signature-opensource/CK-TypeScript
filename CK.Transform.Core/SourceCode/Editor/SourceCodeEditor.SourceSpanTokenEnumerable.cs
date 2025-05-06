using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    internal sealed class SourceSpanTokenEnumerable : IEnumerable<SourceToken>
    {
        readonly SourceCodeEditor _editor;
        readonly SourceSpan _span;

        public SourceSpanTokenEnumerable( SourceCodeEditor editor, SourceSpan span )
        {
            _editor = editor;
            _span = span;
        }

        public IEnumerator<SourceToken> GetEnumerator()
        {
            if( _span.IsDetached ) Array.Empty<SourceToken>().GetEnumerator();
            return new Enumerator( _editor, _span.Span.Beg, _span._span.End );
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal sealed class Enumerator : IEnumerator<SourceToken>
        {
            SourceCodeEditor _editor;
            readonly int _end;
            Token? _token;
            int _index;

            public Enumerator( SourceCodeEditor editor, int beg, int end )
            {
                Throw.DebugAssert( beg >= 0 && end >= beg );
                editor._enumerators.Add( this );
                _editor = editor;
                _end = end;
                _index = beg - 1;
            }

            public void Dispose() => _editor._enumerators.Remove( this );

            public SourceToken Current => new SourceToken( _token!, _index );

            public bool MoveNext()
            {
                if( ++_index >= _end ) return false;
                _token = _editor._tokens[_index];
                return true;
            }

            void IEnumerator.Reset() => Throw.NotSupportedException();

            internal bool OnInsertTokens( int eLimit, int delta )
            {
                if( eLimit > _index ) Throw.CKException( $"Dynamic enumerable at {eLimit} has not been observed (current is {_index})." );
                _index += delta;
                return true;
            }

            internal bool OnRemoveTokens( int eLimit, int delta )
            {
                if( eLimit > _index ) Throw.CKException( $"Dynamic enumerable at {eLimit} has not been observed (current is {_index})." );
                _index -= delta;
                return true;
            }

            object IEnumerator.Current => Current;
        }
    }



}
