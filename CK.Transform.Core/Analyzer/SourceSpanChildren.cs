using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CK.Transform.Core;


public class SourceSpanChildren : IEnumerable<SourceSpan>
{
    internal SourceSpan? _firstChild;
    internal SourceSpan? _lastChild;

    /// <summary>
    /// Gets the first child if any.
    /// </summary>
    public SourceSpan? FirstChild => _firstChild;

    /// <summary>
    /// Gets the last child if any.
    /// </summary>
    public SourceSpan? LastChild => _lastChild;

    /// <summary>
    /// Gets whether this contains at least one <see cref="SourceSpan"/>.
    /// </summary>
    [MemberNotNullWhen( true, nameof( FirstChild ), nameof( _firstChild ), nameof( LastChild ), nameof( _lastChild ) )]
    public bool HasChildren => _firstChild != null;

    /// <summary>
    /// Gets the covering span if <see cref="HasChildren"/> is true, <see cref="TokenSpan.Empty"/> otherwise.
    /// </summary>
    public TokenSpan CoveringSpan => HasChildren ? new TokenSpan( _firstChild.Span.Beg, _lastChild.Span.End ) : default;

    internal void Clear()
    {
        while( _firstChild != null ) _firstChild.Detach( withChildren: true );
    }

    internal SourceSpan? DepthFirstChild
    {
        get
        {
            var c = _firstChild;
            if( c == null ) return null;
            var d = c._children._firstChild;
            while( d != null )
            {
                c = d;
                d = d._children._firstChild;
            }
            return c;
        }
    }


    public struct Enumerator : IEnumerator<SourceSpan>
    {
#pragma warning disable IDE0044 // Not readonly to prevent defensive struct copies.
        SourceSpan? _firstChild;
#pragma warning restore IDE0044 // Add readonly modifier
        SourceSpan? _current;

        internal Enumerator( SourceSpan? firstChild )
        {
            _firstChild = firstChild;
        }

        public readonly SourceSpan Current => _current!;

        object IEnumerator.Current => _current!;

        public void Dispose() { }

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

    internal bool TryAdd( SourceSpan? parent, SourceSpan newOne )
    {
        var c = _firstChild;
        if( c == null )
        {
            _firstChild = _lastChild = newOne;
            newOne._parent = parent;
            return true;
        }
        Throw.DebugAssert( _lastChild != null );

        do
        {
            // newOne is after (or immediately after) the current span: skip to the next span.
            if( newOne._span.Beg >= c._span.End )
            {
                Throw.DebugAssert( newOne._span.GetRelationship( c._span ) is (SpanRelationship.Continued | SpanRelationship.Swapped)
                                                                            or (SpanRelationship.Independent | SpanRelationship.Swapped) );
                c = c._nextSibling;
                continue;
            }
            // newOne is before (or immediately before): we found its place.
            if( newOne._span.End <= c._span.Beg )
            {
                Throw.DebugAssert( newOne._span.GetRelationship( c._span ) is SpanRelationship.Continued
                                                                            or SpanRelationship.Independent );
                newOne._nextSibling = c;
                var prev = c._prevSibling;
                newOne._prevSibling = prev;
                if( prev == null ) _firstChild = newOne;
                c._prevSibling = newOne;
                newOne._parent = parent;
                return true;
            }
            // newOne is contained in the span:
            // - Strictly: it becomes one of its children.
            // - Equals: duplicate span error.
            if( c._span.ContainsOrEquals( newOne._span ) )
            {
                if( c._span == newOne._span )
                {
                    return false;
                }
                return c._children.TryAdd( c, newOne );
            }
            // Entering the covering case.
            if( newOne._span.Beg <= c._span.Beg )
            {
                // Finding the last covered span.
                var last = c;
                while( (last = last._nextSibling) != null )
                {
                    if( newOne._span.End <= last._span.Beg )
                    {
                        last = last._prevSibling;
                        break;
                    }
                    if( newOne._span.End < newOne._span.End )
                    {
                        return false;
                    }
                }
                // newOne covers from c to last.
                if( last == null )
                {
                    // If last is null, the end is covered.
                    if( c._prevSibling == null )
                    {
                        // Full covering case.
                        Throw.DebugAssert( _firstChild == c );
                        newOne._children._firstChild = _firstChild;
                        newOne._children._lastChild = _lastChild;
                        newOne._parent = parent;
                        SetParentFrom( _firstChild, newOne );
                        _firstChild = _lastChild = newOne;
                        return true;
                    }
                    // Tail covering case.
                    Throw.DebugAssert( _firstChild != c && c._prevSibling != null );
                    newOne._children._firstChild = c;
                    newOne._children._lastChild = _lastChild;
                    _lastChild = newOne;
                    c._prevSibling._nextSibling = newOne;
                    c._prevSibling = null;
                    SetParentFrom( c, parent );
                    return true;
                }
                Throw.DebugAssert( last._nextSibling != null );
                newOne._children._firstChild = c;
                newOne._children._lastChild = last;
                if( c._prevSibling != null )
                {
                    c._prevSibling._nextSibling = newOne;
                }
                else
                {
                    _firstChild = newOne;
                }
                c._prevSibling = null;
                last._nextSibling._prevSibling = null;
                SetParentFrom( c, parent );
                return true;
            }
            else
            {
                // Error case: newOne and the existing span overlaps (in any manner but are
                // not equal as this has been handled by the ContainsOrEquals case above).
                Throw.DebugAssert( newOne._span.GetRelationship( c._span ) is SpanRelationship.SameEnd
                                                                            or (SpanRelationship.SameEnd | SpanRelationship.Swapped)
                                                                            or SpanRelationship.SameStart
                                                                            or (SpanRelationship.SameStart | SpanRelationship.Swapped)
                                                                            or SpanRelationship.Overlapped
                                                                            or (SpanRelationship.Overlapped | SpanRelationship.Swapped)
                                                                            or SpanRelationship.Equal );
                return false;
            }
        }
        while( c != null );
        // Not found, no error: the new span is after the existing ones.
        newOne._prevSibling = _lastChild;
        _lastChild._nextSibling = newOne;
        _lastChild = newOne;
        newOne._parent = parent;
        return true;

    }

    internal void SetRoot( SourceSpanRoot root )
    {
        var c = _firstChild;
        while( c != null )
        {
            c._root = root;
            c._children.SetRoot( root );
            c = c._nextSibling;
        }
    }

    internal static void SetParentFrom( SourceSpan? c, SourceSpan? parent )
    {
        Throw.DebugAssert( c != null );
        do
        {
            c._parent = parent;
            c = c._nextSibling;
        }
        while( c != null );
    }

}

