using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Semantically immutable object. <see cref="Beg"/> and <see cref="End"/> are actually mutable in 
/// terms of path (but not in terms of positions): the goal is, whenever possible, to capture better, 
/// more precise, positions.
/// </summary>
public sealed class NodeLocationRange : INodeLocationRange, INodeLocationRangeInternal
{
    NodeLocation _beg;
    NodeLocation _end;
    internal readonly int EachNumber;
    static int _eachNumberAuto;

    /// <summary>
    /// Empty set.
    /// </summary>
    public static readonly INodeLocationRange EmptySet = new NodeLocationRange();

    /// <summary>
    /// Gets the start of this range.
    /// </summary>
    public NodeLocation Beg => _beg;

    /// <summary>
    /// Gets the end of the range. This is greater than <see cref="Beg"/> and excluded in the range
    /// (or is equal to <see cref="Beg"/> if <see cref="IsLocation"/> is true).
    /// </summary>
    public NodeLocation End => _end;

    private NodeLocationRange()
    {
        _beg = _end = null;
    }

    /// <summary>
    /// Initializes a new <see cref="NodeLocationRange"/>.
    /// </summary>
    /// <param name="beg">The start of the range.</param>
    /// <param name="end">The excluded end of the range.</param>
    /// <param name="eachNumber">Optional number that identifies this range in a set of range.</param>
    public NodeLocationRange( NodeLocation beg, NodeLocation end, int eachNumber = 0 )
    {
        Throw.CheckNotNullArgument( beg );
        Throw.CheckArgument( "Range can not include the BegMarker.", !beg.IsBegMarker );
        Throw.CheckNotNullArgument( end );
        int w = end.Position - beg.Position;
        Throw.CheckOutOfRangeArgument( "Range: beg position is after end.", w >= 0 );
        _beg = beg;
        _end = w == 0 ? beg : end;
        // Using an ever increasing range number when none is provided.
        EachNumber = eachNumber >= 0 ? eachNumber : Interlocked.Increment( ref _eachNumberAuto );
    }

    /// <summary>
    /// Gets whether this is a 'point' (i.e. <see cref="Beg"/> == <see cref="End"/>) rather than an actual range.
    /// </summary>
    public bool IsLocation => _beg == _end;

    int INodeLocationRange.Count => 1;

    NodeLocationRange INodeLocationRange.First => this;

    NodeLocationRange INodeLocationRange.Last => this;

    /// <summary>
    /// Gets the most precise location that covers this range.
    /// </summary>
    /// <returns>The most precise node's location that covers this range.</returns>
    public NodeLocation GetCoveringLocation()
    {
        var b = Beg.ToFullLocation();
        if( b != _beg ) _beg = b;
        int w = End.Position - b.Position;
        if( w == 0 ) return _end = _beg;
        // Here b can never be null since Beg can not be the BegMarker: the width
        // is at most the root node's width.
        while( b.Node.Width < w ) b = b.Parent;
        return b;
    }

    /// <summary>
    /// Returns <see cref="GetCoveringLocation"/> only if it exactly covers this range.
    /// </summary>
    /// <returns>The exact qualified location or null.</returns>
    public NodeLocation? GetExactCoveringLocation()
    {
        NodeLocation c = GetCoveringLocation();
        return c.Node.Width == End.Position - _beg.Position ? c : null;
    }

    /// <summary>
    /// Returns the intersection between this range and the other one.
    /// </summary>
    /// <param name="other">The other range.</param>
    /// <returns>The intersection (can be the <see cref="EmptySet"/>).</returns>
    public NodeLocationRange Intersect( NodeLocationRange other )
    {
        Throw.CheckNotNullArgument( other );
        return this == EmptySet || IsLocation || other == EmptySet || other.IsLocation
                        ? (NodeLocationRange)EmptySet
                        : (NodeLocationRange)Unified( this, other, DoIntersect );
    }

    /// <summary>
    /// Returns the union between this range and the other one.
    /// </summary>
    /// <param name="other">The other range.</param>
    /// <returns>The union (can be composed of multiple ranges).</returns>
    public INodeLocationRange Union( NodeLocationRange other )
    {
        Throw.CheckNotNullArgument( other );
        return this == EmptySet || IsLocation
                    ? other
                    : (other == EmptySet || other.IsLocation
                            ? this
                            : Unified( this, other, DoUnion ));
    }

    /// <summary>
    /// Returns a range that is this one excepts the other one.
    /// </summary>
    /// <param name="other">The other range.</param>
    /// <returns>The result that can be composed of multiple ranges.</returns>
    public INodeLocationRange Except( NodeLocationRange other )
    {
        Throw.CheckNotNullArgument( other );
        return this == EmptySet || IsLocation || other == EmptySet || other.IsLocation
                    ? this
                    : Unified( this, other, DoExcept );
    }

    /// <summary>
    /// Always this range since this is not a multiple range.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<NodeLocationRange> GetEnumerator() => new CKEnumeratorMono<NodeLocationRange>( this );

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns the most precise range when the position of <see cref="Beg"/> and <see cref="End"/> are 
    /// the same, otherwise this.
    /// </summary>
    /// <param name="other">The other range.</param>
    /// <returns>This, other or a more precise range at the same position.</returns>
    public NodeLocationRange MostPrecise( NodeLocationRange other )
    {
        //TODO: handle IsLocation case... or totally remove Precision if eventually useless.
        return this;
    }

    static internal INodeLocationRange Create( IEnumerable<NodeLocationRange> ranges, int countOfRanges, bool cloneOnMulti = true )
    {
        if( countOfRanges == 0 ) return EmptySet;
        if( countOfRanges == 1 ) return ranges.First();
        if( cloneOnMulti ) return new LocationRangeList( ranges.ToArray() );
        return new LocationRangeList( ranges is not IReadOnlyList<NodeLocationRange> r ? ranges.ToArray() : r );
    }

    internal NodeLocationRange InternalSetEnd( NodeLocation end )
    {
        Debug.Assert( end.Position >= _beg.Position );
        return new NodeLocationRange( _beg, end );
    }

    internal NodeLocationRange InternalSetBeg( NodeLocation beg )
    {
        Debug.Assert( beg.Position <= _end.Position );
        return new NodeLocationRange( beg, _end );
    }

    INodeLocationRangeInternal INodeLocationRangeInternal.InternalSetEnd( NodeLocation end ) => InternalSetEnd( end );

    INodeLocationRangeInternal INodeLocationRangeInternal.InternalSetEachNumber( int value ) => InternalSetEachNumber( value );

    internal NodeLocationRange InternalSetEachNumber( int value = -1 )
    {
        return value == EachNumber ? this : new NodeLocationRange( _beg, _end, value );
    }

    /// <summary>
    /// Overridden to return the range: "∅" for the empty set, "]location[" for a point, and "[beg,end["
    /// for a regular range.
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString()
    {
        Debug.Assert( Beg != null || this == EmptySet );
        Debug.Assert( (Beg == null) == (End == null) );
        if( this == EmptySet ) return "∅";
        if( IsLocation ) return string.Format( "]{0}[", Beg.Position );
        return string.Format( "[{0},{1}[", Beg.Position, End.Position );
    }

    internal enum Kind
    {
        Equal,
        SameEnd,
        SameStart,
        Congruent,
        Independent,
        Overlapped,
        Contained,
        Swapped = 32
    }

    static internal INodeLocationRange Unified( NodeLocationRange r1,
                                                NodeLocationRange r2,
                                                Func<Kind, NodeLocationRange, NodeLocationRange, INodeLocationRange> on )
    {
        Debug.Assert( r1 != null && r1 != EmptySet && r2 != null && r2 != EmptySet );
        if( r1.Beg.Position == r2.Beg.Position )
        {
            if( r1.End.Position == r2.End.Position ) return on( Kind.Equal, r1, r2 );
            if( r1.End.Position < r2.End.Position ) return on( Kind.SameStart, r1, r2 );
            return on( Kind.SameStart | Kind.Swapped, r2, r1 );
        }
        Kind swap = 0;
        if( r1.Beg.Position > r2.Beg.Position )
        {
            var rTemp = r2;
            r2 = r1;
            r1 = rTemp;
            swap = Kind.Swapped;
        }
        if( r1.End.Position == r2.End.Position )
        {
            return on( Kind.SameEnd | swap, r1, r2 );
        }
        if( r1.End.Position == r2.Beg.Position )
        {
            return on( Kind.Congruent | swap, r1, r2 );
        }
        if( r1.End.Position < r2.Beg.Position )
        {
            return on( Kind.Independent | swap, r1, r2 );
        }
        if( r1.End.Position > r2.End.Position )
        {
            return on( Kind.Contained | swap, r1, r2 );
        }
        return on( Kind.Overlapped | swap, r1, r2 );
    }

    static INodeLocationRange DoIntersect( Kind k, NodeLocationRange r1, NodeLocationRange r2 )
    {
        int eachNumber = r1.EachNumber + r2.EachNumber;
        switch( k & ~Kind.Swapped )
        {
            case Kind.Equal: return r1.MostPrecise( r2 ).InternalSetEachNumber( eachNumber );
            case Kind.Contained: return r2.InternalSetEachNumber( eachNumber );
            case Kind.SameStart: return r1.Beg.ComparePathLength( r2.Beg ) >= 0 ? r1.InternalSetEachNumber( eachNumber ) : new NodeLocationRange( r2.Beg, r1.End, eachNumber );
            case Kind.SameEnd: return r2.End.ComparePathLength( r1.End ) >= 0 ? r2.InternalSetEachNumber( eachNumber ) : new NodeLocationRange( r2.Beg, r1.End, eachNumber );
            case Kind.Overlapped: return new NodeLocationRange( r2.Beg, r1.End, eachNumber );
            case Kind.Congruent:
            case Kind.Independent: return EmptySet;
        }
        throw new NotImplementedException();
    }

    static INodeLocationRange DoUnion( Kind k, NodeLocationRange r1, NodeLocationRange r2 )
    {
        switch( k & ~Kind.Swapped )
        {
            case Kind.Equal: return r1.MostPrecise( r2 );
            case Kind.Contained: return r1;
            case Kind.SameStart: return new NodeLocationRange( r1.Beg.MostPrecise( r2.Beg ), r2.End );
            case Kind.SameEnd: return new NodeLocationRange( r1.Beg, r1.End.MostPrecise( r2.End ) );
            case Kind.Overlapped:
            case Kind.Congruent: return new NodeLocationRange( r1.Beg, r2.End );
            case Kind.Independent: return new LocationRangeCombined( r1, r2 );
        }
        throw new NotImplementedException();
    }

    static INodeLocationRange DoExcept( Kind k, NodeLocationRange r1, NodeLocationRange r2 )
    {
        switch( k )
        {
            case Kind.Equal:
            case Kind.Contained | Kind.Swapped: return EmptySet;

            case Kind.Congruent:
            case Kind.Independent: return r1;

            case Kind.Congruent | Kind.Swapped:
            case Kind.Independent | Kind.Swapped: return r2;

            case Kind.Contained:
            {
                var first = new NodeLocationRange( r1.Beg, r2.Beg );
                var last = new NodeLocationRange( r2.End, r1.End );
                return new LocationRangeCombined( first, last );
            }

            case Kind.SameStart: return EmptySet;
            case Kind.SameStart | Kind.Swapped: return new NodeLocationRange( r1.End, r2.End );

            case Kind.Overlapped:
            case Kind.SameEnd: return new NodeLocationRange( r1.Beg, r2.Beg );
            case Kind.SameEnd | Kind.Swapped: return EmptySet;
            case Kind.Overlapped | Kind.Swapped: return new NodeLocationRange( r1.End, r2.End ); ;
        }
        throw new NotImplementedException();
    }


}
