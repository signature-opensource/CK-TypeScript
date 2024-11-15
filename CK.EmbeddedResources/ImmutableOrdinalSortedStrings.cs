using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

namespace CK.Core;

/// <summary>
/// Captures an ordered array of strings.
/// Strings don't have to be unique.
/// </summary>
public readonly struct ImmutableOrdinalSortedStrings
{
    /// <summary>
    /// The whole array of strings.
    /// </summary>
    public readonly ImmutableArray<string> All;

    /// <summary>
    /// Initializes a new <see cref="ImmutableOrdinalSortedStrings"/> from any array.
    /// </summary>
    /// <param name="strings">The strings.</param>
    public ImmutableOrdinalSortedStrings( params string[] strings )
        : this( true, (string[])strings.Clone() )
    {
    }

    /// <summary>
    /// Initializes a new <see cref="ImmutableOrdinalSortedStrings"/> from a set of strings.
    /// </summary>
    /// <param name="strings">The strings.</param>
    public ImmutableOrdinalSortedStrings( IEnumerable<string> strings )
        : this( true, strings.ToArray() )
    {
    }

    /// <summary>
    /// Creates a <see cref="ImmutableOrdinalSortedStrings"/> on an existing array whose ownership
    /// is transfered to the result: the array must not be altered in any way.
    /// </summary>
    /// <param name="transferOwnership">Array to transfer.</param>
    /// <param name="mustSort">True to sort the array, false if the array is already sorted.</param>
    /// <returns>The strings.</returns>
    public static ImmutableOrdinalSortedStrings UnsafeCreate( string[] transferOwnership, bool mustSort )
    {
        return mustSort
                ? new ImmutableOrdinalSortedStrings( true, transferOwnership )
                : new ImmutableOrdinalSortedStrings( true, true, transferOwnership );
    }

    ImmutableOrdinalSortedStrings( bool ownsIt, string[] strings )
    {
        Array.Sort( strings, StringComparer.Ordinal );
        All = ImmutableCollectionsMarshal.AsImmutableArray( strings );
    }

    ImmutableOrdinalSortedStrings( bool ownsIt, bool sorted, string[] strings )
    {
        All = ImmutableCollectionsMarshal.AsImmutableArray( strings );
    }

    /// <summary>
    /// Gets whether <see cref="All"/> can be used or has not been initialized.
    /// </summary>
    public bool IsValid => !All.IsDefault;

    /// <summary>
    /// Gets the index of the string in <see cref="All"/> or -1 if not found.
    /// </summary>
    /// <param name="name">Name to search.</param>
    /// <returns>The resulting index or -1 if not found.</returns>
    public int IndexOf( string name )
    {
        Throw.CheckNotNullArgument( name );
        return IndexOf( name, All.AsSpan() );
    }

    readonly struct Finder : IComparable<string>
    {
        readonly string _prefix;

        public Finder( string prefix ) => _prefix = prefix;

        public int CompareTo( string? other )
        {
            Throw.DebugAssert( other != null );
            return _prefix.AsSpan().CompareTo( other.AsSpan(), StringComparison.Ordinal );
        }
    }

    internal static int IndexOf( string name, ReadOnlySpan<string> sAll )
    {
        int idx = sAll.BinarySearch( new Finder( name ) );
        return idx < 0 ? -1 : idx;
    }

    readonly struct BegFinder : IComparable<string>
    {
        readonly string _prefix;

        public BegFinder( string prefix ) => _prefix = prefix;

        public int CompareTo( string? other )
        {
            Throw.DebugAssert( other != null );
            if( _prefix.Length > other.Length ) return 1;
            int cmp = _prefix.AsSpan().CompareTo( other.AsSpan( 0, _prefix.Length ), StringComparison.Ordinal );
            return cmp == 0 ? -1 : cmp;
        }
    }

    /// <summary>
    /// Gets the index of the first string in <see cref="All"/> that starts with the <paramref name="prefix"/>.
    /// </summary>
    /// <param name="prefix">Prefix to search.</param>
    /// <returns>The resulting index or -1 if not found.</returns>
    public int GetPrefixedStart( string prefix ) => GetPrefixedStart( prefix, All.AsSpan() );

    internal static int GetPrefixedStart( string prefix, ReadOnlySpan<string> sAll )
    {
        int beg = sAll.BinarySearch( new BegFinder( prefix ) );
        if( beg < 0 )
        {
            beg = ~beg;
            if( beg == sAll.Length ) return -1;
        }
        return beg;
    }

    readonly struct EndFinder : IComparable<string>
    {
        readonly string _prefix;

        public EndFinder( string prefix ) => _prefix = prefix;

        public int CompareTo( string? other )
        {
            Throw.DebugAssert( other != null );
            if( _prefix.Length > other.Length ) return -1;
            int cmp = _prefix.AsSpan().CompareTo( other.AsSpan( 0, _prefix.Length ), StringComparison.Ordinal );
            return cmp == 0 ? 1 : cmp;
        }
    }

    /// <summary>
    /// Gets the range of strings in <see cref="All"/> that start with the <paramref name="prefix"/>.
    /// </summary>
    /// <param name="prefix">Common prefix to search.</param>
    /// <returns>The resulting range. Length (and Idx) is 0 if no strings can be found.</returns>
    public (int Idx, int Length) GetPrefixedRange( string prefix ) => GetPrefixedRange( prefix, All.AsSpan() );

    internal static (int Idx, int Length) GetPrefixedRange( string prefix, ReadOnlySpan<string> sAll )
    {
        int beg = sAll.BinarySearch( new BegFinder( prefix ) );
        if( beg < 0 )
        {
            beg = ~beg;
            if( beg == sAll.Length ) return default;
        }
        int len = sAll.Slice( beg ).BinarySearch( new EndFinder( prefix ) );
        if( len < 0 )
        {
            len = ~len;
        }
        return (beg, len);
    }


    /// <summary>
    /// Gets the range of strings in <see cref="All"/> that start with the <paramref name="prefix"/>.
    /// </summary>
    /// <param name="prefix">Common prefix to search.</param>
    /// <returns>The resulting strings.</returns>
    public ReadOnlyMemory<string> GetPrefixedStrings( string prefix )
    {
        var (i, l) = GetPrefixedRange( prefix );
        return All.AsMemory().Slice( i, l );
    }

}
