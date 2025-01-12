using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Line number and column in a source text. This uses a 1 based index.
/// <see cref="IsDefault"/> detects the <c>default</c> value: there is no need to use <see cref="Nullable{T}"/>
/// for this type.
/// <para>
/// Static helpers <see cref="GetSourceIndex(ReadOnlySpan{char})"/> and <see cref="GetSourcePosition(ReadOnlySpan{char}, int)"/>
/// associate any position to a character in a text.
/// </para>
/// </summary>
public readonly struct SourcePosition : IEquatable<SourcePosition>, IComparable<SourcePosition>
{
    readonly int _line;
    readonly int _column;

    /// <summary>
    /// Initializes a new instance of a <see cref="SourcePosition"/>.
    /// </summary>
    /// <param name="line">Must be positive.</param>
    /// <param name="column">Must be positive.</param>
    public SourcePosition( int line, int column )
    {
        Throw.CheckOutOfRangeArgument( line > 0 );
        Throw.CheckOutOfRangeArgument( column > 0 );
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
    /// Gets whether this is the invalid <c>default</c> (0,0) position.
    /// </summary>
    public bool IsDefault => _line != 0;

    /// <summary>
    /// Gets the index in the source text that corresponds to this position or null
    /// if this position doesn't exist.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <returns>The index or null if not found.</returns>
    public int? GetSourceIndex( ReadOnlySpan<char> source )
    {
        int result = 0;
        var l = _line;
        while( --l > 0 )
        {
            int nextIdx = source.Slice( result ).IndexOf( '\n' );
            if( nextIdx < 0 ) return null;
            result += nextIdx;
        }
        int lineLength = source.Slice( result ).IndexOf( '\n' );
        if( lineLength < 0 ) lineLength = source.Length - result;
        return _column < lineLength ? result + _column : null;
    }

    /// <summary>
    /// Computes the <see cref="SourcePosition"/> in a source text.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="index">The index in the <paramref name="source"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">index is less than zero or greater than source's length</exception>
    /// <returns>The source position.</returns>
    public static SourcePosition GetSourcePosition( ReadOnlySpan<char> source, int index )
    {
        int line, column;
        var before = source.Slice( 0, index );
        int lastIndex = before.LastIndexOf( '\n' );
        if( lastIndex >= 0 )
        {
            line = before.Count( '\n' );
            if( lastIndex > 0 && before[lastIndex - 1] == '\r' ) ++lastIndex;
            column = index - lastIndex;
        }
        else
        {
            line = 0;
            column = index;
        }
        return new SourcePosition( line + 1, column + 1 );
    }

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
    /// Provides a string representation for <see cref="SourcePosition"/>:
    /// "(1,1)" for the first character.
    /// </summary>
    public override string ToString() => $"{Line},{Column}";

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
