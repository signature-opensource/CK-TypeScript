using CK.Core;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Linked list of non overlapping <see cref="SourceSpan"/>.
/// </summary>
public partial class SourceSpanChildren : IEnumerable<SourceSpan>
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

    /// <summary>
    /// Calls <see cref="SourceSpan.CheckValid()"/> on these children.
    /// </summary>
    /// <returns>True if all children are valid.</returns>
    public bool CheckValid()
    {
        var f = _firstChild;
        while( f != null )
        {
            if( !f.CheckValid() ) return false;
            f = f._nextSibling;
        }
        return true;
    }

    internal void Clear()
    {
        while( _firstChild != null ) _firstChild.Detach( withChildren: true );
    }

    internal bool TryAdd( SourceSpan? parent, SourceSpan newOne )
    {
        var c = _firstChild;
        if( c == null )
        {
            Throw.DebugAssert( _lastChild == null );
            _firstChild = _lastChild = newOne;
            newOne._parent = parent;
            return true;
        }
        Throw.DebugAssert( _lastChild != null );

        do
        {
            Throw.DebugAssert( c._parent == parent );
            // newOne is after (or immediately after) the current span: skip to the next span.
            if( newOne._span.Beg >= c._span.End )
            {
                Throw.DebugAssert( newOne._span.GetRelationship( c._span ) is (SpanRelationship.Contiguous | SpanRelationship.Swapped)
                                                                            or (SpanRelationship.Independent | SpanRelationship.Swapped) );
                c = c._nextSibling;
                continue;
            }
            // newOne is before (or immediately before): we found its place.
            if( newOne._span.End <= c._span.Beg )
            {
                Throw.DebugAssert( newOne._span.GetRelationship( c._span ) is SpanRelationship.Contiguous
                                                                            or SpanRelationship.Independent );
                newOne._nextSibling = c;
                var prev = c._prevSibling;
                newOne._prevSibling = prev;
                if( prev == null ) _firstChild = newOne;
                else prev._nextSibling = newOne;
                c._prevSibling = newOne;
                newOne._parent = parent;
                return true;
            }
            if( c._span.Contains( newOne._span ) )
            {
                // newOne is strictly contained in the span: it becomes one of its children.
                return c._children.TryAdd( c, newOne );
            }
            // Entering the covering case.
            if( newOne._span.Beg <= c._span.Beg )
            {
                // Replace case: newOne ends at the current span,
                // we found its place: it replaces the current span and
                // the current span becomes its child.
                if( newOne._span.End == c._span.End )
                {
                    Replace( parent, newOne, c );
                    return true;
                }
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
                // not equal as this has been handled by the Replace case above).
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

    void Replace( SourceSpan? parent, SourceSpan newOne, SourceSpan c )
    {
        newOne._nextSibling = c._nextSibling;
        newOne._prevSibling = c._prevSibling;
        if( _firstChild == c )
        {
            Throw.DebugAssert( c._prevSibling == null );
            _firstChild = newOne;
        }
        else
        {
            Throw.DebugAssert( c._prevSibling != null );
            c._prevSibling._nextSibling = newOne;
            c._prevSibling = null;
        }
        if( _lastChild == c )
        {
            Throw.DebugAssert( c._nextSibling == null );
            _lastChild = newOne;
        }
        else
        {
            Throw.DebugAssert( c._nextSibling != null );
            c._nextSibling._prevSibling = newOne;
            c._nextSibling = null;
        }
        newOne.Children._firstChild = c;
        newOne.Children._lastChild = c;
        newOne._parent = parent;
        c._parent = newOne;
    }

    internal SourceSpan? GetSpanAt( int index )
    {
        var c = _firstChild;
        while( c != null )
        {
            if( c.Span.Contains( index ) )
            {
                return c.GetSpanAt( index );
            }
            c = c._nextSibling;
        }
        return null;
    }

    internal void OnInsertTokens( int index, int count, bool insertBefore )
    {
        Throw.DebugAssert( index >= 0 && count > 0 );
        var c = _firstChild;
        while( c != null )
        {
            if( index < c.Span.Beg || (insertBefore && index == c.Span.Beg) )
            {
                c._span = new TokenSpan( c.Span.Beg + count, c.Span.End + count );
                c._children.OnInsertTokens( index, count, insertBefore );
            }
            else if( index < c.Span.End )
            {
                c._span = new TokenSpan( c.Span.Beg, c.Span.End + count );
                c._children.OnInsertTokens( index, count, insertBefore );
            }
            c = c._nextSibling;
        }
    }

    internal void OnRemoveTokens( TokenSpan removedHead, ref List<SourceSpan>? toRemove )
    {
        var c = _firstChild;
        while( c != null )
        {
            var s = Remove( c.Span, removedHead );
            if( s.IsEmpty )
            {
                toRemove ??= new List<SourceSpan>();
                toRemove.Add( c );
            }
            else if( s != c.Span )
            {
                c._span = s;
                c._children.OnRemoveTokens( removedHead, ref toRemove );
            }
            c = c._nextSibling;
        }
    }

    static TokenSpan Remove( TokenSpan span, TokenSpan removed )
    {
        return span.GetRelationship( removed ) switch
        {
            // The span must be removed.
            SpanRelationship.Equal
                or SpanRelationship.Contained|SpanRelationship.Swapped
                or SpanRelationship.SameStart
                or SpanRelationship.SameEnd|SpanRelationship.Swapped => TokenSpan.Empty,
            // [...][XXX]No change (span is before removed).
            SpanRelationship.Independent
                or SpanRelationship.Contiguous => span,
            // [XXX][...] Offset (removed is before span).
            SpanRelationship.Independent|SpanRelationship.Swapped
                or SpanRelationship.Contiguous|SpanRelationship.Swapped => new TokenSpan( span.Beg - removed.Length, span.End - removed.Length ),
            // [...[XXX]]
            SpanRelationship.SameEnd => new TokenSpan( span.Beg, removed.Beg ),
            // [[XXX]...]
            SpanRelationship.SameStart|SpanRelationship.Swapped => new TokenSpan( removed.End, span.End ),
            // [...[XXX]...]
            SpanRelationship.Contained => new TokenSpan( span.Beg, span.End - removed.Length ),
            // [...[X.X.X]X]
            SpanRelationship.Overlapped => new TokenSpan( span.Beg, removed.Beg ),
            // [X[X.X.X]...]
            SpanRelationship.Overlapped|SpanRelationship.Swapped => new TokenSpan( removed.End, span.End ),
            _ => Throw.NotSupportedException<TokenSpan>()
        };
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

