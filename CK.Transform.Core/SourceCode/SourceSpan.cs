using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Span of a source code. Specialized instances are created by analyzers and
/// managed by <see cref="SourceCode"/> and <see cref="SourceCodeEditor"/>.
/// </summary>
public abstract partial class SourceSpan
{
    internal SourceSpan? _parent;
    internal SourceSpan? _firstChild;
    internal SourceSpan? _lastChild;
    internal SourceSpan? _prevSibling;
    internal SourceSpan? _nextSibling;
    internal TokenSpan _span;

    /// <summary>
    /// Initializes a new SourceSpan.
    /// </summary>
    /// <param name="beg">The start of the span. Must be greater or equal to 0.</param>
    /// <param name="end">The end of the span. Must be greater than <paramref name="beg"/>.</param>
    protected SourceSpan( int beg, int end )
    {
        _span = new TokenSpan( beg, end );
    }

    /// <summary>
    /// Gets whether this is the root: its <see cref="Span"/> is the <see cref="TokenSpan.Infinite"/>.
    /// </summary>
    public bool IsRoot => _span == TokenSpan.Infinite;

    /// <summary>
    /// Gets whether this span is the root or doesn't belong
    /// to a <see cref="SourceCode"/>.
    /// </summary>
    [MemberNotNullWhen( false, nameof( Parent ), nameof( _parent ) )]
    public bool IsDetached => _parent == null;

    /// <summary>
    /// Gets the children spans.
    /// </summary>
    public IEnumerable<SourceSpan> Children
    {
        get
        {
            var c = _firstChild;
            while( c != null )
            {
                yield return c;
                c = c._nextSibling;
            }
        }
    }

    public IEnumerable<SourceSpan> AllSpans
    {
        get
        {
            foreach( var s in Children )
            {
                yield return s;
                foreach( var sub in s.AllSpans )
                {
                    yield return sub;
                }
            }
        }
    }

    /// <summary>
    /// Checks whether this span is valid. At this level, it must not be <see cref="IsDetached"/>
    /// and all children's <see cref="SourceSpanChildren.CheckValidChildren()"/> must return true.
    /// <para>
    /// This is often specialized to implement checks on <see cref="Children"/>, <see cref="Parent"/>
    /// or siblings. Specializations must call this base method.
    /// </para>
    /// <para>
    /// Overrides typically specify [MemberNotNullWhen( true, ...)] with required properties of the structure.
    /// This has been designed to be used in <see cref="Throw.DebugAssert(bool, string?, string?, int)"/>:
    /// spans must always be valid, see <see cref="Bind"/>.
    /// </para>
    /// </summary>
    /// <returns>True if this span is valid.</returns>
    public virtual bool CheckValid()
    {
        return !IsDetached && CheckValidChildren();
    }

    /// <summary>
    /// Gets the deepest span at a given position.
    /// <para>
    /// <see cref="Parent"/> can be used to retrieve all the parent covering spans.
    /// </para>
    /// </summary>
    /// <param name="index">The token index.</param>
    /// <returns>The deepest span or null if no children span covers the token at this position.</returns>
    public SourceSpan? GetChildrenSpanAt( int index )
    {
        var c = _firstChild;
        while( c != null )
        {
            if( index < c.Span.Beg ) break;
            if( index < c.Span.End )
            {
                return c.GetChildrenSpanAt( index ) ?? c;
            }
            c = c._nextSibling;
        }
        return null;
    }

    public enum DetachMode
    {
        ClearChildren,
        LiftChilderen,
        KeepChildren
    }

    /// <summary>
    /// Detaches this span from the <see cref="Root"/>.
    /// </summary>
    /// <param name="withChildren">
    /// True to also detach the <see cref="Children"/> instead of lifting them
    /// as children of this <see cref="Parent"/>.
    /// </param>
    public void Detach( DetachMode mode )
    {
        if( _parent == null ) return;

        if( mode == DetachMode.ClearChildren )
        {
            ClearChildren();
        }
        else if( mode == DetachMode.LiftChilderen && HasChildren )
        {
            SetParentFrom( _firstChild, _parent );
            _nextSibling = _firstChild;
            _prevSibling = _lastChild;
        }
        if( _prevSibling == null )
        {
            _parent._firstChild = _nextSibling;
        }
        else
        {
            _prevSibling._nextSibling = _nextSibling;
        }
        if( _nextSibling == null )
        {
            _parent._lastChild = _prevSibling;
        }
        else
        {
            _nextSibling._prevSibling = _prevSibling;
        }
        _parent.CheckInvariants();
        _nextSibling = null;
        _prevSibling = null;
        _parent = null;
    }

    /// <summary>
    /// Gets the token span.
    /// </summary>
    public TokenSpan Span => _span;

    /// <summary>
    /// Gets the parent span.
    /// Null when <see cref="IsDetached"/> or <see cref="IsRoot"/>.
    /// </summary>
    public SourceSpan? Parent => _parent;

    /// <summary>
    /// Gets the previous sibling.
    /// </summary>
    public SourceSpan? PreviousSibling => _prevSibling;

    /// <summary>
    /// Gets the next sibling.
    /// </summary>
    public SourceSpan? NextSibling => _nextSibling;

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
    public TokenSpan ChildrenCoveringSpan => HasChildren ? new TokenSpan( _firstChild.Span.Beg, _lastChild.Span.End ) : default;

    /// <summary>
    /// Calls <see cref="CheckValid()"/> on the children.
    /// </summary>
    /// <returns>True if all children are valid.</returns>
    protected bool CheckValidChildren()
    {
        var f = _firstChild;
        while( f != null )
        {
            if( !f.CheckValid() ) return false;
            f = f._nextSibling;
        }
        return true;
    }

    /// <summary>
    /// Gets the type name of this span. This should be compared using <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// <para>
    /// Defaults to this type name.
    /// </para>
    /// </summary>
    public virtual string TypeName => GetType().Name;

    /// <summary>
    /// Overridden to return the <see cref="TypeName"/> and the <see cref="Span"/>.
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString() => $"{TypeName} {_span}";

    internal SourceSpan? GetRoot() => _parent == null
                                        ? IsRoot
                                            ? this
                                            : null
                                        : _parent.GetRoot();

    internal bool DoTryAdd( SourceSpan newOne )
    {
        var c = _firstChild;
        if( c == null )
        {
            Throw.DebugAssert( _lastChild == null );
            _firstChild = _lastChild = newOne;
            newOne._parent = this;
            return true;
        }
        Throw.DebugAssert( _lastChild != null );

        do
        {
            Throw.DebugAssert( c._parent == this );
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
                newOne._parent = this;
                return true;
            }
            if( c._span.Contains( newOne._span ) )
            {
                // newOne is strictly contained in the span: it becomes one of its children.
                return c.DoTryAdd( newOne );
            }
            // Entering the covering case.
            if( newOne._span.Beg <= c._span.Beg )
            {
                //// Substitute case: newOne ends at the current span,
                //// we found its place: it replaces the current span and
                //// the current span becomes its child.
                //if( newOne._span.End == c._span.End )
                //{
                //    Substitute( c, newOne );
                //    return true;
                //}
                // Finding the last covered span.
                var last = c;
                while( (last = last._nextSibling) != null )
                {
                    if( newOne._span.End <= last._span.Beg )
                    {
                        last = last._prevSibling;
                        break;
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
                        newOne._firstChild = _firstChild;
                        newOne._lastChild = _lastChild;
                        _firstChild = _lastChild = newOne;
                    }
                    else
                    {
                        // Tail covering case.
                        Throw.DebugAssert( _firstChild != c && c._prevSibling != null );
                        newOne._firstChild = c;
                        newOne._lastChild = _lastChild;
                        newOne._prevSibling = c._prevSibling;
                        _lastChild = newOne;
                        c._prevSibling._nextSibling = newOne;
                        c._prevSibling = null;
                    }
                }
                else
                {
                    Throw.DebugAssert( last._nextSibling != null );
                    newOne._firstChild = c;
                    newOne._lastChild = last;
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
                }
                newOne._parent = this;
                SetParentFrom( c, newOne );
                return true;
            }
            else
            {
                // Error case: newOne and the existing span overlaps.
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
        newOne._parent = this;
        return true;

    }

    internal void DoOnInsertTokens( int index, int count, bool insertBefore )
    {
        Throw.DebugAssert( index >= 0 && count > 0 );
        var c = _firstChild;
        while( c != null )
        {
            if( index < c.Span.Beg || (insertBefore && index == c.Span.Beg) )
            {
                c._span = c._span.Offset( count );
                c.DoOnInsertTokens( index, count, insertBefore );
            }
            else if( index < c.Span.End )
            {
                c._span = new TokenSpan( c.Span.Beg, c.Span.End + count );
                c.DoOnInsertTokens( index, count, insertBefore );
            }
            c = c._nextSibling;
        }
    }

    internal void DoOnRemoveTokens( TokenSpan removedHead, ref List<SourceSpan>? toRemove )
    {
        var c = _firstChild;
        while( c != null )
        {
            var s = c.Span.Remove( removedHead );
            if( s.IsEmpty )
            {
                toRemove ??= new List<SourceSpan>();
                toRemove.Add( c );
            }
            else if( s != c.Span )
            {
                c._span = s;
                c.DoOnRemoveTokens( removedHead, ref toRemove );
            }
            c = c._nextSibling;
        }
    }

    internal void ClearChildren()
    {
        while( _firstChild != null ) _firstChild.Detach( DetachMode.ClearChildren );
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

    [Conditional( "DEBUG" )]
    internal void CheckInvariants()
    {
        Throw.CheckState( (_firstChild == null) == (_lastChild == null) );
        if( _firstChild != null )
        {
            Throw.DebugAssert( _lastChild != null );
            Throw.CheckState( _firstChild._prevSibling == null );
            Throw.CheckState( _lastChild._nextSibling == null );
            var c = _firstChild;
            while( c != null )
            {
                Throw.CheckState( c._parent == this );
                Throw.CheckState( _span.ContainsOrEquals( c._span ) );
                c.CheckInvariants();
                var n = c._nextSibling;
                Throw.CheckState( (n == null) == (c == _lastChild) );
                Throw.CheckState( n == null || n._prevSibling == c );
                Throw.CheckState( n == null || c._span.GetRelationship( n._span ) is SpanRelationship.Independent or SpanRelationship.Contiguous );
                c = n;
            }
        }
    }

}
