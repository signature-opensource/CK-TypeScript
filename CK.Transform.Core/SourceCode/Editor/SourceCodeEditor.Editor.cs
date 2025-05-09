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
                // Reparse what must be reparsed.
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
    }
}
