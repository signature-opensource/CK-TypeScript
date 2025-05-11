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
        List<Token>? _enumTokens;
        Token? _token;
        int _index;

        public SourceTokenEnumerable( SourceCodeEditor editor )
        {
            _editor = editor;
            _index = -1;
        }

        [MemberNotNullWhen( false, nameof( _enumTokens ) )]
        bool IsDisposed => _enumTokens == null;

        public IEnumerator<SourceToken> GetEnumerator()
        {
            Throw.CheckState( "Multiple use of the UnfilteredSourceTokens.", IsDisposed );
            _index = -1;
            _enumTokens = _editor._tokens;
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            _enumTokens = null;
            _index = -1;
        }

        public SourceToken Current => new SourceToken( _token!, _index );

        public bool MoveNext()
        {
            Throw.CheckState( !IsDisposed );
            if( ++_index >= _enumTokens.Count ) return false;
            _token = _enumTokens[_index];
            return true;
        }

        internal void OnInsertTokens( int eLimit, int delta )
        {
            if( _index >= 0 )
            {
                if( eLimit > _index ) ThrowUnobserved( eLimit );
                _index += delta;
            }
        }

        internal void OnRemoveTokens( int eLimit, int count )
        {
            if( _index >= 0 )
            {
                if( eLimit > _index ) ThrowUnobserved( eLimit );
                _index -= count;
            }
        }

        internal void OnUpdateTokens( int eLimit )
        {
            if( _index >= 0 && eLimit > _index ) ThrowUnobserved( eLimit );
        }

        void ThrowUnobserved( int eLimit )
        {
            Throw.CKException( $"Source tokens enumerable at {eLimit} has not been observed (current is {_index})." );
        }

        object IEnumerator.Current => Current;

        void IEnumerator.Reset() => Throw.NotSupportedException();
    }

}
