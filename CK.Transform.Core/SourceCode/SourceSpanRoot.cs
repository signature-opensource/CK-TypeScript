using CK.Core;
using System.Collections;
using System.Collections.Generic;

namespace CK.Transform.Core;


sealed class SourceSpanRoot : SourceSpan, ISourceSpanRoot
{
    public SourceSpanRoot()
        : base( 0, int.MaxValue )
    {
    }

    IEnumerator<SourceSpan> IEnumerable<SourceSpan>.GetEnumerator() => Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Children.GetEnumerator();

    public override bool CheckValid() => CheckValidChildren();

    /// <summary>
    /// Adds a span or throws if it intersects any existing children.
    /// </summary>
    /// <param name="newOne">The span to add.</param>
    public void Add( SourceSpan newOne )
    {
        if( !TryAdd( newOne ) )
        {
            Throw.ArgumentException( nameof( newOne ), $"Invalid new '{newOne}'." );
        }
    }

    /// <summary>
    /// Adds a span with an updated <see cref="SourceSpan.Span"/>.
    /// Same as <see cref="Add(SourceSpan)"/>: this throws if the updated span intersects
    /// any existing children.
    /// </summary>
    /// <param name="newOne">The span to add.</param>
    /// <param name="newSpan">Token span to update.</param>
    public void Add( SourceSpan newOne, TokenSpan newSpan )
    {
        newOne._span = newSpan;
        if( !TryAdd( newOne ) )
        {
            Throw.ArgumentException( nameof( newOne ), $"Invalid new '{newOne}'." );
        }
    }

    /// <summary>
    /// Adds a span if it doesn't intersect any existing children.
    /// </summary>
    /// <param name="newOne">The span to add.</param>
    /// <returns>True on success, false if the span intersects an existing span.</returns>
    public bool TryAdd( SourceSpan newOne )
    {
        Throw.CheckArgument( newOne.IsDetached && !newOne.IsRoot );
        CheckInvariants();
        bool r = DoTryAdd( newOne );
        CheckInvariants();
        return r;
    }

    internal void TransferTo( SourceSpanRoot target )
    {
        Throw.DebugAssert( "Not called if useless.", HasChildren );
        Throw.DebugAssert( "Supported Transfer is only an Append.", target.ChildrenCoveringSpan.End <= ChildrenCoveringSpan.Beg );

        CheckInvariants();

        if( target.HasChildren )
        {
            target._lastChild._nextSibling = _firstChild;
            _firstChild._prevSibling = target._lastChild;
            target._lastChild = _lastChild;
        }
        else
        {
            target._firstChild = _firstChild;
            target._lastChild = _lastChild;
        }
        SetParentFrom( _firstChild, target );
        _firstChild = null;
        _lastChild = null;

        CheckInvariants();
    }

    internal void MoveSpanBefore( SourceSpan span, SourceSpan target )
    {
        Throw.DebugAssert( span != target
                           && target.Span.GetRelationship( span.Span ) is SpanRelationship.Independent or SpanRelationship.Contiguous );
        Throw.DebugAssert( !span.IsDetached && !target.IsDetached );

        CheckInvariants();

        var movedTokenSpan = span.Span;

        // Secure any span that exactly match the moved one: it will be the real moved span.
        // If we don't do this, these "exact covering" parents will be removed when we'll "remove"
        // the tokens.
        var actualMoved = span;
        while( actualMoved._parent != null && actualMoved._parent.Span == movedTokenSpan )
        {
            actualMoved = actualMoved._parent;
        }
        // Detach the actualMoved.
        actualMoved.Detach( DetachMode.KeepChildren );
        Throw.DebugAssert( "Detach keeps the span.", !actualMoved.Span.IsEmpty );

        int lenOffset = actualMoved.Span.Beg - target.Span.Beg;
        // Offset the span of the now detached span.
        actualMoved._span = actualMoved._span.Offset( -lenOffset );
        // And update its children.
        List<SourceSpan>? toRemove = null;
        actualMoved.DoOnRemoveTokens( new TokenSpan( 0, lenOffset ), ref toRemove );
        Throw.DebugAssert( "All the spans here are after the removed span.", toRemove == null );

        // "Remove" its tokens: all spans after be offsetted... for nothing because when we'll
        // "add" the tokens back before, they will be offsetted back to their original position.
        // TODO: improve this by walking the structure to offset only the touched spans...
        DoOnRemoveTokens( movedTokenSpan, ref toRemove );
        Throw.DebugAssert( "We detached any spans that may have been removed.", toRemove == null );

        // We don't need to find an exact covering span for the target.
        // We simply "insert" the moved tokens right before the target spans: we call
        // the root DoOnInsertTokens here.
        DoOnInsertTokens( target.Span.Beg, actualMoved.Span.Length, insertBefore: true );

        // Then we attach the moved span before the target.
        actualMoved._parent = target._parent;
        actualMoved._nextSibling = target;
        if( target._prevSibling == null )
        {
            target._parent._firstChild = actualMoved;
        }
        else
        {
            actualMoved._prevSibling = target._prevSibling;
            target._prevSibling._nextSibling = actualMoved;
        }
        target._prevSibling = actualMoved;

        CheckInvariants();
    }

    internal void OnInsertTokens( int index, int count, bool insertBefore )
    {
        CheckInvariants();
        DoOnInsertTokens( index, count, insertBefore );
        CheckInvariants();
    }

    internal List<SourceSpan>? OnRemoveTokens( TokenSpan removedHead )
    {
        CheckInvariants();
        List<SourceSpan>? toRemove = null;
        DoOnRemoveTokens( removedHead, ref toRemove );
        if( toRemove != null )
        {
            foreach( var s in toRemove )
            {
                s.Detach( DetachMode.ClearChildren );
            }
        }
        CheckInvariants();
        return toRemove;
    }

}
