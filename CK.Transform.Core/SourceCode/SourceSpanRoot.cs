using CK.Core;
using System.Collections;
using System.Collections.Generic;

namespace CK.Transform.Core;

public sealed class SourceSpanRoot : IEnumerable<SourceSpan>
{
    internal readonly SourceSpanChildren _children;

    public SourceSpanRoot()
    {
        _children = new SourceSpanChildren();
    }

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

    /// <summary>
    /// Gets the deepest span at a given position.
    /// <para>
    /// <see cref="SourceSpan.Parent"/> can be used to retrieve all the parent covering spans.
    /// </para>
    /// </summary>
    /// <param name="index">The token index.</param>
    /// <returns>The deepest span or null if no span covers the token at this position.</returns>
    public SourceSpan? GetSpanAt( int index ) => _children.GetSpanAt( index );

    /// <summary>
    /// Gets a span enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public SourceSpanChildren.Enumerator GetEnumerator() => _children.GetEnumerator();

    IEnumerator<SourceSpan> IEnumerable<SourceSpan>.GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

    internal void OnAddTokens( int index, int count ) => _children.OnAddTokens( index, count );

    internal void OnRemoveTokens( int index, int count )
    {
        Throw.DebugAssert( index >= 0 && count > 0 );
        List<SourceSpan>? toRemove = null;
        _children.OnRemoveTokens( new TokenSpan( index, index + count ), ref toRemove );
        if( toRemove != null )
        {
            foreach( var s in toRemove )
            {
                s.Detach( withChildren: true );
            }
        }
    }
}
