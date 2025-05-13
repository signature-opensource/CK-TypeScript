using CK.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    public enum OpenEditorState { None, Global, Scoped }

    sealed class Editor : IScopedCodeEditor
    {
        readonly SourceCodeEditor _e;
        LinkedTokenOperatorContext _currentFilter;
        FilteredTokenSpanDynamicEnumerator _filteredTokens;

        OpenEditorState _openState;

        internal Editor( SourceCodeEditor e )
        {
            _e = e;
            // Root operator.
            _currentFilter = new LinkedTokenOperatorContext( e );
            // Unitialized.
            _filteredTokens = new FilteredTokenSpanDynamicEnumerator();
        }

        internal OpenEditorState OpenState => _openState;

        internal ICodeEditor OpenGlobal()
        {
            Throw.CheckState( OpenState is OpenEditorState.None );
            _openState = OpenEditorState.Global;
            return this;
        }

        internal IScopedCodeEditor OpenScoped()
        {
            Throw.CheckState( OpenState is OpenEditorState.None );
            _openState = OpenEditorState.Scoped;
            _filteredTokens.Reset( _currentFilter.FilteredTokens, _e._tokens );
            return this;
        }

        internal void PushTokenOperator( IFilteredTokenOperator filterProvider )
        {
            _currentFilter = new LinkedTokenOperatorContext( _e, filterProvider, _currentFilter );
        }

        internal LinkedTokenOperatorContext CurrentFilter => _currentFilter;

        internal void PopTokenOperator( int count )
        {
            while( --count >= 0 )
            {
                Throw.DebugAssert( _currentFilter.Previous != null );
                _currentFilter = _currentFilter.Previous;
            }
        }

        public void Dispose()
        {
            if( !_e._hasError )
            {
                // Reparse what must be reparsed.
            }
        }

        public IReadOnlyList<Token> UnfilteredTokens => _e._tokens;

        public IFilteredTokenSpanEnumerator Tokens
        {
            get
            {
                Throw.CheckState( OpenState is OpenEditorState.Scoped );
                return _filteredTokens;
            }
        }

        public void Replace( int index, params ReadOnlySpan<Token> tokens ) => Replace( index, tokens.Length, tokens );

        public void Replace( int index, int count, params ReadOnlySpan<Token> tokens )
        {
            Throw.CheckArgument( !tokens.IsEmpty );
            Throw.CheckArgument( count > 0 );
            _e.DoReplace( index, count, tokens );
        }

        public void InsertAt( int index, params ReadOnlySpan<Token> tokens )
        {
            Throw.CheckArgument( !tokens.IsEmpty );
            _e.DoReplace( index, 0, tokens );
        }

        public void InsertBefore( int index, params ReadOnlySpan<Token> tokens )
        {
            Throw.CheckArgument( tokens.Length > 0 );
            _e.DoReplace( index, 0, tokens, insertBefore: true );
        }

        public void RemoveAt( int index )
        {
            _e.DoReplace( index, 1, default );
        }

        public void RemoveRange( int index, int count )
        {
            Throw.CheckArgument( count > 0 );
            _e.DoReplace( index, count, default );
        }

        internal void OnUpdateTokens( int eLimit, int delta )
        {
            _filteredTokens.OnUpdateTokens( eLimit, delta );
        }

    }
}
