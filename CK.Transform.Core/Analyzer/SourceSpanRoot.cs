using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed class SourceSpanRoot
{
    internal readonly SourceSpanChildren _children;

    public SourceSpanRoot()
    {
        _children = new SourceSpanChildren();
    }

    public IEnumerable<SourceSpan> Spans => _children;

    /// <summary>
    /// Adds a span or throws if it intersects any existing <see cref="Children"/>.
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
    /// Adds a span if it doesn't intersect any existing <see cref="Children"/>.
    /// </summary>
    /// <param name="newOne">The span to add.</param>
    /// <returns>True on success, false if the span intersects an existing span.</returns>
    public bool TryAdd( SourceSpan newOne )
    {
        Throw.CheckArgument( newOne.IsDetached );
        if( _children.TryAdd( null, newOne ) )
        {
            newOne._root = this;
            return true;
        }
        return false;
    }

    internal void TransferTo( SourceSpanRoot target )
    {
        Throw.DebugAssert( "Not called if useless.", _children.HasChildren );
        Throw.DebugAssert( "Supported Transfer is only an Append.", target._children.CoveringSpan.End <= _children.CoveringSpan.Beg );
        _children.SetRoot( target );
        if( target._children.HasChildren )
        {
            target._children._lastChild._nextSibling = _children._firstChild;
            _children._firstChild._prevSibling = target._children._lastChild;
            target._children._lastChild = _children._lastChild;
        }
        else
        {
            target._children._firstChild = _children._firstChild;
            target._children._lastChild = _children._lastChild;
        }
        _children._firstChild = null;
        _children._lastChild = null;
    }
}
