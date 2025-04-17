using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Models a [<see cref="Beg"/>,<see cref="End"/>[ span of tokens.
/// </summary>
public readonly struct TokenSpan : IEquatable<TokenSpan>
{
    readonly int _beg;
    readonly int _end;

    /// <summary>
    /// The empty span is the <c>default</c>.
    /// </summary>
    public static readonly TokenSpan Empty = default;

    /// <summary>
    /// Initializes a non empty span.
    /// </summary>
    /// <param name="beg">The start of the span. Must be greater or equal to 0.</param>
    /// <param name="end">The end of the span. Must be greater than <paramref name="beg"/>.</param>
    public TokenSpan( int beg, int end )
    {
        Throw.CheckArgument( beg >= 0 && beg < end );
        _beg = beg;
        _end = end;
    }

    /// <summary>
    /// Gets whether this is the <see cref="Empty"/> span.
    /// </summary>
    public bool IsEmpty => _end == 0;

    /// <summary>
    /// Gets the length of this span. Always positive except for the <see cref="Empty"/>.
    /// </summary>
    public int Length => _end - _beg;

    /// <summary>
    /// Gets the start of this span. Always greater or equal to 0.
    /// </summary>
    public int Beg => _beg;

    /// <summary>
    /// Gets the excluded end of this span. Except <see cref="Empty"/> for which it is 0, this is always
    /// greater than <see cref="Beg"/>.
    /// </summary>
    public int End => _end;

    /// <summary>
    /// Gets whether the <paramref name="index"/> is in [<see cref="Beg"/>,<see cref="End"/>[.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>Whether this span contains the index.</returns>
    public bool Contains( int index ) => index >= _beg && index < _end;

    /// <summary>
    /// Gets whether the <paramref name="other"/> is contained in or is [<see cref="Beg"/>,<see cref="End"/>[.
    /// </summary>
    /// <param name="other">The span.</param>
    /// <returns>Whether this span contains or is equal to other.</returns>
    public bool ContainsOrEquals( TokenSpan other ) => other._beg >= _beg && other._end <= _end;

    /// <summary>
    /// Gets whether [<see cref="Beg"/>,<see cref="End"/>[ strictly contains <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The span.</param>
    /// <returns>Whether this span contains other.</returns>
    public bool Contains( TokenSpan other ) => ContainsOrEquals( other ) && !Equals( other );

    /// <summary>
    /// Whether <see cref="Beg"/> and <see cref="End"/> are equals.
    /// </summary>
    /// <param name="other">The other span.</param>
    /// <returns>True if this is equal to other.</returns>
    public bool Equals( TokenSpan other ) => _beg == other._beg && _end == other._end;

    /// <summary>
    /// Deconstructs this span.
    /// </summary>
    /// <param name="beg">The <see cref="Beg"/>.</param>
    /// <param name="end">The <see cref="End"/>.</param>
    public void Deconstruct( out int beg, out int end )
    {
        beg = _beg;
        end = _end;
    }

    /// <summary>
    /// Computes the relationship between this first span and a second one.
    /// When <see cref="SpanRelationship.Swapped"/> bit is set, the two spans MUST be exchanged.
    /// </summary>
    /// <param name="s">Second span.</param>
    /// <returns>The relationship between this first and the second span.</returns>
    public SpanRelationship GetRelationship( TokenSpan s ) => GetRelationship( this, s );

    /// <summary>
    /// Computes the relationship between two spans.
    /// When <see cref="SpanRelationship.Swapped"/> bit is set, the two spans MUST be exchanged.
    /// </summary>
    /// <param name="s1">First span.</param>
    /// <param name="s2">Second span.</param>
    /// <returns>The relationship between the first and the second span.</returns>
    public static SpanRelationship GetRelationship( TokenSpan s1, TokenSpan s2 ) => GetRelationship( ref s1, ref s2 );

    /// <summary>
    /// Computes the relationship between two spans.
    /// When <see cref="SpanRelationship.Swapped"/> bit is set, the two spans have been exchanged.
    /// </summary>
    /// <param name="r1">First span.</param>
    /// <param name="r2">Second span.</param>
    /// <returns>The relationship between the first and the second span.</returns>
    public static SpanRelationship GetRelationship( ref TokenSpan r1, ref TokenSpan r2 )
    {
        if( r1._beg == r2._beg )
        {
            if( r1._end == r2._end ) return SpanRelationship.Equal;
            if( r1._end < r2._end )
            {
                // If r1 is the empty span, they are considered independent.
                if( r1._end == 0 ) return SpanRelationship.Independent;
                return SpanRelationship.SameStart;
            }
            return SpanRelationship.SameStart | SpanRelationship.Swapped;
        }
        SpanRelationship swap = 0;
        if( r1._beg > r2._beg )
        {
            (r1, r2) = (r2, r1);
            // If r2 is the empty span, they are independent (but r2 comes first).
            if( r2._end == 0 ) return SpanRelationship.Independent | SpanRelationship.Swapped;
            swap = SpanRelationship.Swapped;
        }
        if( r1._end == r2._end )
        {
            return SpanRelationship.SameEnd | swap;
        }
        if( r1._end == r2._beg )
        {
            return SpanRelationship.Contiguous | swap;
        }
        if( r1._end < r2._beg )
        {
            return SpanRelationship.Independent | swap;
        }
        if( r1._end > r2._end )
        {
            return SpanRelationship.Contained | swap;
        }
        return SpanRelationship.Overlapped | swap;
    }

    /// <summary>
    /// Gets "∅" for an empty span, "[<see cref="Beg"/>,<see cref="End"/>[" otherwise.
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString() => IsEmpty ? "∅" : $"[{_beg},{_end}[";

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public override bool Equals( object? obj ) => obj is TokenSpan r && Equals( r );

    public override int GetHashCode() => _beg ^ (int)((uint)_end << 16 | (uint)_end >> 16);

    public static bool operator ==( TokenSpan left, TokenSpan right ) => left.Equals( right );

    public static bool operator !=( TokenSpan left, TokenSpan right ) => !(left == right);
}
