using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Awful beast that is used for lookup in the index.
/// </summary>
sealed class MutableRPSetKey : IRPSetKey
{
    [AllowNull] int[] _indexes;
    int _hash;
    int _start;
    int _length;

    public void Reset( int[] indexes, int start, int length )
    {
        _indexes = indexes;
        SetSpan( start, length );
    }

    public void SetSpan( int start, int length )
    {
        _start = start;
        _length = length;
        int iMax = start + length;
        HashCode c = new HashCode();
        for( int i = start; i < iMax; ++i )
        {
            c.Add( _indexes[i] );
        }
        _hash = c.ToHashCode();
    }

    public int GetSinglePackageIndex()
    {
        Throw.DebugAssert( _length == 1 );
        var i = _indexes[_start];
        return i < 0 ? ~i : i;
    }

    public int Length => _length;

    public ReadOnlySpan<int> PackageIndexes => _indexes.AsSpan( _start, _length );

    public override bool Equals( object? obj )
    {
        Throw.DebugAssert( "There cannot be 2 MutableRPSetKey at the same time.",
                            obj is not MutableRPSetKey );
        return obj is ORPSetKey k
                ? PackageIndexes.SequenceEqual( k.PackageIndexes )
                : false;
    }

    public override int GetHashCode() => _hash;
}
