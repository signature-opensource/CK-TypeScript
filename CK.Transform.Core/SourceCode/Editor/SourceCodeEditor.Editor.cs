using CK.Core;
using System;
using System.Collections.Generic;
using static CK.Transform.Core.SourceCodeEditor;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    public sealed class Editor : IDisposable
    {
        readonly SourceCodeEditor _e;
        readonly SourceTokenEnumerable _sourceTokens;
        // This stack is managed at the SourceCodeEditor level: filters can only be pushed/pop
        // when this editor is not opened.
        internal readonly Stack<IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> _tokenFilters;
        int _openCount;


        internal Editor( SourceCodeEditor e )
        {
            _e = e;
            _sourceTokens = new SourceTokenEnumerable( e );
            _tokenFilters = new Stack<IEnumerable<IEnumerable<IEnumerable<SourceToken>>>>();
            _tokenFilters.Push( [[_sourceTokens]] );
        }

        internal int OpenCount => _openCount;

        internal Editor Open() { ++_openCount; return this; }

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
        public IEnumerable<SourceToken> UnfilteredTokens => _sourceTokens;

        /// <summary>
        /// Enumerates the filtered <see cref="SourceToken"/> by each and all groups.
        /// </summary>
        public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Tokens => _tokenFilters.Peek();

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
