using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    /// <summary>
    /// Implementation <see cref="ScopedTokens"/>.
    /// </summary>
    public sealed class TokenScope
    {
        readonly Stack<IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> _tokenFilters;
        readonly SourceCodeEditor _editor;

        internal TokenScope( SourceCodeEditor editor )
        {
            _tokenFilters = new Stack<IEnumerable<IEnumerable<IEnumerable<SourceToken>>>>();
            _tokenFilters.Push( [[editor._code.SourceTokens]] );
            _editor = editor;
        }

        /// <summary>
        /// Enumerates the scoped <see cref="SourceToken"/> by each and all groups.
        /// <para>
        /// Note that the enumerator MUST be disposed once done with it because it contains
        /// a <see cref="ImmutableList{T}.Enumerator"/> that must be disposed.
        /// </para>
        /// </summary>
        public IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Tokens => _tokenFilters.Peek();

        /// <summary>
        /// Enumerates the flattened tokens from the <see cref="ScopedTokens"/>.
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
        /// Pushes a new <see cref="ITokenFilter"/> on <see cref="Tokens"/>.
        /// </summary>
        /// <param name="monitor">The monitor that will detects errors.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <returns>True on sucess, false on error.</returns>
        public bool PushTokenFilter( IActivityMonitor monitor, ITokenFilter filter )
        {
            var builder = new ScopedTokensBuilder( monitor, _editor );
            var f = filter.GetScopedTokens( builder );
            if( !builder.HasError )
            {
                _tokenFilters.Push( f );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Pops the last pushed token filter.
        /// </summary>
        public void PopTokenFilter()
        {
            Throw.DebugAssert( _tokenFilters.Count > 1 );
            _tokenFilters.Pop();
        }
    }

}
