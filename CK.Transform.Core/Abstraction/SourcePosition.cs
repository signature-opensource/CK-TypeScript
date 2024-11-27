using CK.Core;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace CK.Transform.Core;

/// <summary>
/// Line number and column in a source text.
/// This uses 0 based index.
/// </summary>
public readonly struct SourcePosition : IEquatable<SourcePosition>, IComparable<SourcePosition>
{
    readonly int _line;
    readonly int _column;

    /// <summary>
    /// Initializes a new instance of a <see cref="SourcePosition"/>.
    /// </summary>
    /// <param name="line">Must be 0 (first line) or positive.</param>
    /// <param name="column">Must be 0 (first character) or positive.</param>
    public SourcePosition( int line, int column )
    {
        Throw.CheckOutOfRangeArgument( line >= 0 );
        Throw.CheckOutOfRangeArgument( column >= 0 );
        _line = line;
        _column = column;
    }

    /// <summary>
    /// Gets the 0 based line number.
    /// </summary>
    public int Line => _line;

    /// <summary>
    /// Gets the 0 based column.
    /// </summary>
    public int Column => _column;

    /// <summary>
    /// Determines whether two <see cref="SourcePosition"/> are the same.
    /// </summary>
    public static bool operator ==( SourcePosition left, SourcePosition right ) => left.Equals( right );

    /// <summary>
    /// Determines whether two <see cref="SourcePosition"/> are different.
    /// </summary>
    public static bool operator !=( SourcePosition left, SourcePosition right ) => !left.Equals( right );

    /// <summary>
    /// Determines whether two <see cref="SourcePosition"/> are the same.
    /// </summary>
    /// <param name="other">The object to compare.</param>
    public bool Equals( SourcePosition other ) => other._line == _line && other._column == _column;

    /// <summary>
    /// Determines whether two <see cref="SourcePosition"/> are the same.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    public override bool Equals( object? obj ) => obj is SourcePosition && Equals( (SourcePosition)obj );

    /// <summary>
    /// Provides a hash function for <see cref="SourcePosition"/>.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine( _line, _column );

    /// <summary>
    /// Provides a string representation for <see cref="SourcePosition"/>.
    /// </summary>
    /// <example>0,10</example>
    public override string ToString() => Line + "," + Column;

    /// <summary>
    /// Compares the two positions.
    /// </summary>
    /// <param name="other">The other one.</param>
    /// <returns>Standard comparison result.</returns>
    public int CompareTo( SourcePosition other )
    {
        var cmp = _line.CompareTo( other._line );
        return (cmp != 0)
                ? cmp
                : _column.CompareTo( other._column );
    }

    public static bool operator >( SourcePosition left, SourcePosition right ) => left.CompareTo( right ) > 0;

    public static bool operator >=( SourcePosition left, SourcePosition right ) => left.CompareTo( right ) >= 0;

    public static bool operator <( SourcePosition left, SourcePosition right ) => left.CompareTo( right ) < 0;

    public static bool operator <=( SourcePosition left, SourcePosition right ) => left.CompareTo( right ) <= 0;
}
