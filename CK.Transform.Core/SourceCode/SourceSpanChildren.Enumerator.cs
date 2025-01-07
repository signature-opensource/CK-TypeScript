using System.Collections;
using System.Collections.Generic;

namespace CK.Transform.Core;


public partial class SourceSpanChildren
{
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

        public readonly SourceSpan Current => _current!;

        readonly object IEnumerator.Current => _current!;

        public readonly void Dispose() { }

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

        public void Reset() => _current = null;
    }

    public Enumerator GetEnumerator() => new Enumerator( _firstChild );

    IEnumerator<SourceSpan> IEnumerable<SourceSpan>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

