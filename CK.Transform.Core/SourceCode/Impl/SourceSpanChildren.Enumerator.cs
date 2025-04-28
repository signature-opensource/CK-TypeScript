using System;
using System.Collections;
using System.Collections.Generic;

namespace CK.Transform.Core;


public partial class SourceSpanChildren
{
    /// <summary>
    /// Children SourceSpan enumerator.
    /// </summary>
    public struct Enumerator : IEnumerator<SourceSpan>
    {
        // Not readonly to prevent defensive struct copies.
#pragma warning disable IDE0044
        SourceSpan? _firstChild;
#pragma warning restore IDE0044
        SourceSpan? _current;

        internal Enumerator( SourceSpan? firstChild )
        {
            _firstChild = firstChild;
        }

        /// <summary>
        /// Gets the current SourceSpan.
        /// </summary>
        public readonly SourceSpan Current => _current!;

        readonly object IEnumerator.Current => _current!;

        /// <summary>
        /// Empty implementation.
        /// </summary>
        public readonly void Dispose() { }

        /// <summary>
        /// Moves to the next SourceSpan.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if( _current == null )
            {
                _current = _firstChild;
                return _current != null;
            }
            var c = _current._nextSibling;
            if( c != null )
            {
                _current = c;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets this enumerator.
        /// </summary>
        public void Reset() => _current = null;
    }

    /// <summary>
    /// Gets an enumerator on children.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public Enumerator GetEnumerator() => new Enumerator( _firstChild );

    IEnumerator<SourceSpan> IEnumerable<SourceSpan>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}

