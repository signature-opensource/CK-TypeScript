using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Immutable capture of a node, its parent node and absolute position (in terms of tokens).
/// </summary>
public class NodeLocation : IComparable<NodeLocation>
{
    /// <summary>
    /// The beginning of input marker.
    /// </summary>
    public static readonly TokenNode BegOfInput = TokenNode.CreateMarker( NodeType.BegOfInput );

    /// <summary>
    /// The end of input marker.
    /// </summary>
    public static readonly TokenNode EndOfInput = TokenNode.CreateMarker( NodeType.EndOfInput );

    /// <summary>
    /// The node can be null (ie. unknown) when this location is returned by <see cref="Successor"/>
    /// or <see cref="Predecessor"/>.
    /// </summary>
    public readonly AbstractNode? Node;

    /// <summary>
    /// The node parent. Never null except for the root node.
    /// </summary>
    public readonly NodeLocation? Parent;

    /// <summary>
    /// The associated position.
    /// </summary>
    public readonly int Position;

    /// <summary>
    /// Initializes a new location.
    /// </summary>
    /// <param name="parent">Parent of the node. Can not be null.</param>
    /// <param name="node">The node. Null for a raw location.</param>
    /// <param name="p">The associated position.</param>
    internal NodeLocation( NodeLocation parent, AbstractNode? node, int p )
    {
        Throw.DebugAssert( parent != null );
        Throw.DebugAssert( "Parent node is null.", parent.Node != null );
        Parent = parent;
        Node = node;
        Position = p;
    }

    /// <summary>
    /// Root objects internal constructor.
    /// </summary>
    internal NodeLocation( AbstractNode node )
    {
        Throw.DebugAssert( node != null );
        Node = node;
    }

    /// <summary>
    /// Gets whether this location's <see cref="Node"/> is not null, only the <see cref="Position"/> is known.
    /// </summary>
    public bool IsQualifiedLocation => Node != null;

    /// <summary>
    /// Gets whether this location is a full one: the <see cref="Node"/> is the ending token.
    /// A full location is a qualified one.
    /// </summary>
    public bool IsFullLocation => Node is TokenNode;

    /// <summary>
    /// Returns the qualified location of a node at this <see cref="Position"/>.
    /// The node must exist at this position otherwise an <see cref="ArgumentException"/> is thrown. 
    /// </summary>
    /// <returns>The qualified location.</returns>
    public NodeLocation ToQualifiedLocation( AbstractNode node ) => Node == node ? this : GetRoot().GetQualifiedLocation( Position, node );

    /// <summary>
    /// Returns the full location for this <see cref="Position"/> (the full path to the token terminal node).
    /// </summary>
    /// <returns>
    /// This location if <see cref="IsFullLocation"/> is true, the full location for this <see cref="Position"/> otherwise.
    /// </returns>
    public NodeLocation ToFullLocation() => IsFullLocation ? this : GetRoot().GetFullLocation( Position );

    /// <summary>
    /// Gets whether this is the special <see cref="BegOfInput"/> marker.
    /// </summary>
    public bool IsBegMarker => Node == BegOfInput;

    /// <summary>
    /// Gets whether this is the special <see cref="EndOfInput"/> marker.
    /// </summary>
    public bool IsEndMarker => Node == EndOfInput;

    /// <summary>
    /// Compares this location to another one. The comparison is based on the <see cref="Position"/>, and, when the 
    /// two positions are equal, on the parent relationships: a child (more precise) is greater than a parent.
    /// </summary>
    /// <param name="other">The location to compare to.</param>
    /// <returns>Positive if this is greater than other.</returns>
    public int CompareTo( NodeLocation? other ) => CompareTo( other, false );

    /// <summary>
    /// Compares this location to another one. The comparison is based on the <see cref="Position"/>, and, when the 
    /// two positions are equal, on the length of the path.
    /// </summary>
    /// <param name="other">The location to compare to.</param>
    /// <param name="parentIsGreater">True to consider shorter path to be better than a longer one (when position are the same).</param>
    /// <returns>Standard negative/0/positive value.</returns>
    public int CompareTo( NodeLocation? other, bool parentIsGreater )
    {
        int cmp = 1;
        if( other != null )
        {
            cmp = Position - other.Position;
            if( cmp == 0 )
            {
                if( Node != other.Node )
                {
                    cmp = ComparePathLength( other );
                    if( parentIsGreater ) cmp = -cmp;
                }
            }
        }
        return cmp;
    }

    /// <summary>
    /// Compares the path length (number of non null <see cref="Node"/>).
    /// </summary>
    /// <param name="other">Other location.</param>
    /// <returns>1 if this path is longer that the other one, 0 or -1 otherwise.</returns>
    public int ComparePathLength( NodeLocation? other )
    {
        if( other == null ) return 1;
        var pO = other.Parent;
        if( pO != null && pO.Node != null ) pO = pO.Parent;
        var pT = Parent;
        if( pT != null && pT.Node != null ) pT = pT.Parent;
        for(; ; )
        {
            if( pO == null ) return pT == null ? 0 : 1;
            if( pT == null ) return -1;
            pO = pO.Parent;
            pT = pT.Parent;
        }
    }

    /// <summary>
    /// Gets the greatest location between this and another one. If the two <see cref="Position"/> are the same,
    /// the most precise one is returned.
    /// </summary>
    /// <param name="other">Other location to challenge.</param>
    /// <returns>This or other.</returns>
    public NodeLocation Max( NodeLocation? other )
    {
        return CompareTo( other, false ) >= 0 ? this : other!;
    }

    /// <summary>
    /// Gets the lowest location between this and another one. If the two <see cref="Position"/> are the same,
    /// the most precise one is returned.
    /// </summary>
    /// <param name="other">Other location to challenge.</param>
    /// <returns>This or other.</returns>
    public NodeLocation Min( NodeLocation? other )
    {
        return CompareTo( other, true ) <= 0 ? this : (other ?? this);
    }

    /// <summary>
    /// Returns the most precise location when the two <see cref="Position"/> are the same, otherwise this.
    /// </summary>
    /// <param name="other">The other location. Must not be null.</param>
    /// <returns>This or the other if positions are the same and other has a longer path than this.</returns>
    public NodeLocation MostPrecise( NodeLocation other )
    {
        return Position != other.Position || ComparePathLength( other ) >= 0 ? this : other;
    }

    /// <summary>
    /// Returns a location on the previous token.
    /// This is null if <see cref="IsBegMarker"/> is true.
    /// </summary>
    /// <param name="fullLocation">True to obtain the full location.</param>
    /// <returns>A location on the previous token.</returns>
    public NodeLocation? Predecessor( bool fullLocation = false )
    {
        if( IsBegMarker ) return null;
        if( IsEndMarker ) return GetRoot().GetLastLocation( fullLocation );
        if( Position == 0 ) return GetRoot().BegMarker;
        Throw.DebugAssert( "Handled by Position == 0 above.", Parent != null );
        if( fullLocation ) return GetRoot().GetFullLocation( Position - 1 );
        var p = Parent;
        while( p.Position == Position )
        {
            p = p.Parent;
            Throw.DebugAssert( p != null );
        }
        return new NodeLocation( p, null, Position - 1 );
    }

    /// <summary>
    /// Returns a location on the next token. This is null if <see cref="IsEndMarker"/> is true.
    /// </summary>
    /// <param name="fullLocation">True to obtain the full location.</param>
    /// <returns>A location on the next token.</returns>
    public NodeLocation? Successor( bool fullLocation = false )
    {
        if( IsBegMarker ) return fullLocation ? GetRoot().GetFullLocation( 0 ) : GetRoot();
        if( IsEndMarker ) return null;
        if( fullLocation ) return GetRoot().GetFullLocation( Position + 1 );
        var p = Parent;
        Throw.DebugAssert( p != null || (Position == 0 && this is LocationRoot) );
        int newPos = Position + 1;
        if( p == null ) return Node.Width == newPos ? ((LocationRoot)this).EndMarker : new NodeLocation( this, null, 1 );
        while( p.Position + p.Node.Width <= newPos )
        {
            p = p.Parent;
            if( p == null ) return GetRoot().GetRawLocation( newPos );
        }
        return new NodeLocation( p, null, newPos );
    }

    /// <summary>
    /// Gets the root location node. Never null.
    /// </summary>
    public NodeLocation Root => GetRoot();

    LocationRoot GetRoot()
    {
        NodeLocation l = this;
        while( l.Parent != null ) l = l.Parent;
        return (LocationRoot)l;
    }

    /// <summary>
    /// Get the path from this one up to the root parent.
    /// </summary>
    public IEnumerable<NodeLocation> ReversePath
    {
        get
        {
            var p = this;
            do
            {
                yield return p;
                p = p.Parent;
            }
            while( p != null );
        }
    }

    /// <summary>
    /// Get the path from the root parent up to this one.
    /// </summary>
    public IEnumerable<NodeLocation> Path => ReversePath.Reverse();

    /// <summary>
    /// Overridden to return the path.
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString()
    {
        StringBuilder b = new StringBuilder();
        int curP = -1;
        b.Append( Position ).Append( " - " );
        foreach( var l in Path )
        {
            if( curP >= 0 ) b.Append( '/' );
            b.Append( l.Node == null ? "(null node)" : l.Node.GetType().Name );
            if( curP != l.Position ) b.Append( $"[{l.Position}]" );
            curP = l.Position;
        }
        if( Node != null ) b.Append( " - " ).Append( Node.GetType().Name );
        return b.ToString();
    }
}
