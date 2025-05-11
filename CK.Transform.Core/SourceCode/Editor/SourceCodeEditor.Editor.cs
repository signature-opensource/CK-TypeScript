using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    public sealed class Editor : IDisposable
    {
        readonly SourceCodeEditor _e;
        LinkedTokenFilterBuilderContext _currentFilter;
        int _openCount;

        internal Editor( SourceCodeEditor e )
        {
            _e = e;
            _currentFilter = new LinkedTokenFilterBuilderContext( e );
        }

        internal int OpenCount => _openCount;

        internal Editor Open() { ++_openCount; return this; }

        internal void PushTokenFilter( IFilteredTokenEnumerableProvider filterProvider )
        {
            if( filterProvider != IFilteredTokenEnumerableProvider.Empty )
            {
                _currentFilter = new LinkedTokenFilterBuilderContext( _e, filterProvider, _currentFilter );
            }
        }

        internal LinkedTokenFilterBuilderContext CurrentFilter => _currentFilter;

        internal void PopTokenFilter( int count )
        {
            while( --count >= 0 )
            {
                Throw.DebugAssert( _currentFilter.Previous != null );
                _currentFilter = _currentFilter.Previous;
            }
        }

        public void Dispose()
        {
            if( --_openCount == 0 )
            {
                // Dump the first filtering error if any.
                TokenFilteringError? filteringError = _e._filteringError;
                if( filteringError != null )
                {
                    _e._filteringError = null;
                    filteringError.Dump( _e._monitor );
                    Throw.DebugAssert( _e._hasError );
                }
                // Clears the tracked dynamic spans.
                _e._dynamicSpans.Clear();
                if( !_e._hasError )
                {
                    // Reparse what must be reparsed.
                }
            }
        }

        /// <summary>
        /// Enumerates all <see cref="SourceToken"/>.
        /// </summary>
        public IEnumerable<SourceToken> UnfilteredTokens => _e._sourceTokens;

        /// <summary>
        /// Enumerates the filtered <see cref="SourceToken"/> by each and all groups.
        /// </summary>
        public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Tokens => _currentFilter.Tokens;

        /// <summary>
        /// Enumerates the flattened tokens from the filtered <see cref="Tokens"/>.
        /// </summary>
        public IEnumerable<SourceToken> AllTokens
        {
            get
            {
                foreach( var each in Tokens )
                    foreach( var range in each )
                        foreach( var t in range )
                            yield return t;
            }
        }

        /// <summary>
        /// Replaces the tokens starting at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the first token that must be replaced.</param>
        /// <param name="tokens">Updated tokens. Must not be empty.</param>
        public void Replace( int index, params ReadOnlySpan<Token> tokens ) => Replace( index, tokens.Length, tokens );

        /// <summary>
        /// Replaces one or more tokens with any number of tokens.
        /// </summary>
        /// <param name="index">The index of the first token that must be replaced.</param>
        /// <param name="count">The number of tokens to replace. Must be positive.</param>
        /// <param name="tokens">New tokens to insert. Must not be empty.</param>
        public void Replace( int index, int count, params ReadOnlySpan<Token> tokens )
        {
            Throw.CheckArgument( !tokens.IsEmpty );
            Throw.CheckArgument( count > 0 );
            _e.DoReplace( index, count, tokens );
        }

        /// <summary>
        /// Inserts new tokens. Spans that start at <paramref name="index"/> will contain the inserted tokens.
        /// </summary>
        /// <param name="index">The index of the inserted tokens.</param>
        /// <param name="tokens">New tokens to insert. Must not be empty.</param>
        public void InsertAt( int index, params ReadOnlySpan<Token> tokens )
        {
            Throw.CheckArgument( !tokens.IsEmpty );
            _e.DoReplace( index, 0, tokens );
        }

        /// <summary>
        /// Inserts new tokens. Spans that start at <paramref name="index"/> will not contain the inserted tokens.
        /// </summary>
        /// <param name="index">The index of the inserted tokens.</param>
        /// <param name="tokens">New tokens to insert. Must not be empty.</param>
        public void InsertBefore( int index, params ReadOnlySpan<Token> tokens )
        {
            Throw.CheckArgument( tokens.Length > 0 );
            _e.DoReplace( index, 0, tokens, insertBefore: true );
        }

        /// <summary>
        /// Removes a token at a specified index.
        /// </summary>
        /// <param name="index">The token index to remove.</param>
        public void RemoveAt( int index )
        {
            _e.DoReplace( index, 1, default );
        }

        /// <summary>
        /// Removes a range of tokens.
        /// </summary>
        /// <param name="index">The index of the first token to remove.</param>
        /// <param name="count">The number of tokens to remove. Must be positive.</param>
        public void RemoveRange( int index, int count )
        {
            Throw.CheckArgument( count > 0 );
            _e.DoReplace( index, count, default );
        }
    }
}
