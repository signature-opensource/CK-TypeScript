using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    sealed class DynamicTokenFilterEnumerator : ITokenFilterEnumerator
    {
        TokenFilterEnumeratorImpl _impl;

        public DynamicTokenFilterEnumerator()
        {
        }

        /// <inheritdoc cref="ITokenFilterEnumerator.IsSingleEach"/>
        public bool IsSingleEach => _impl.IsSingleEach;

        /// <inheritdoc cref="ITokenFilterEnumerator.Input"/>
        public IReadOnlyList<TokenMatch> Input => _impl.Input;

        /// <inheritdoc cref="ITokenFilterEnumerator.CurrentInputIndex"/>
        public int CurrentInputIndex => _impl.CurrentInputIndex;

        /// <inheritdoc cref="ITokenFilterEnumerator.CurrentEachIndex"/>
        public int CurrentEachIndex => _impl.CurrentEachIndex;

        /// <inheritdoc cref="ITokenFilterEnumerator.State"/>
        public TokenFilterEnumeratorState State => _impl.State;

        /// <inheritdoc cref="ITokenFilterEnumerator.CurrentMatch"/>
        public TokenMatch CurrentMatch => _impl.CurrentMatch;

        /// <inheritdoc cref="ITokenFilterEnumerator.Token"/>
        public SourceToken Token => _impl.Token;

        /// <inheritdoc cref="ITokenFilterEnumerator.NextEach"/>
        public bool NextEach( bool skipEmpty ) => _impl.NextEach( skipEmpty );

        /// <inheritdoc cref="ITokenFilterEnumerator.NextMatch"/>
        public bool NextMatch() => _impl.NextMatch();

        /// <inheritdoc cref="ITokenFilterEnumerator.NextMatch"/>
        public bool NextMatch( out SourceToken first, out SourceToken last, out int count )
            => _impl.NextMatch( out first, out last, out count );

        /// <inheritdoc cref="ITokenFilterEnumerator.NextToken"/>
        public bool NextToken() => _impl.NextToken();

        internal void Reset( TokenMatch[] matches, IReadOnlyList<Token> tokens )
        {
            _impl.Reset( matches, tokens );
        }

        internal void OnUpdateTokens( int eLimit, int delta )
        {
            if( _impl.State is not TokenFilterEnumeratorState.Finished )
            {
                _impl.OnUpdateTokens( eLimit, delta );
            }
        }

    }

}
