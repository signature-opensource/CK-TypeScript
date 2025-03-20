using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Awful beast that is used to find a <see cref="IReachablePackageSet"/> in the index.
/// </summary>
sealed class MutableRPSetKey : IRPSetKey
{
    [AllowNull] int[] _indexes;
    int _hash;
    int _baseStart;
    int _baseLength;
    int _start;
    int _length;

    internal MutableRPSetKey Reset( int[] indexes, int baseStart, int baseLength )
    {
        Throw.DebugAssert( indexes.Length >= 2 && indexes.IsSortedStrict() );
        _indexes = indexes;
        _baseStart = baseStart;
        _baseLength = baseLength;
        return SetSpan( baseStart, baseLength );
    }

    internal MutableRPSetKey MoveToStable()
    {
        Throw.DebugAssert( _baseStart == 0 && _baseLength >= 2 );
        Throw.DebugAssert( _indexes[_baseStart] < 0 && _indexes[_baseLength] >= 0 );
        _baseStart = _baseLength;
        _baseLength = _indexes.Length - _baseLength;
        return SetSpan( _baseStart, _baseLength );
    }

    internal void SetHole( int prefixLength, int holeSize )
    {
        Throw.DebugAssert( !IsHybrid );
        SetSpan( _start + prefixLength, holeSize );
    }

    internal Snapshot StartLongestSuffix()
    {
        Throw.DebugAssert( !IsHybrid );
        SetSpan( _start + 1, _length - 1 );
        return CreateSnapshot();
    }

    internal void NextLongestSuffix()
    {
        SetSpan( _start + 1, _length - 1 );
    }

    internal Snapshot StartLongestPrefix( int suffixLength )
    {
        Throw.DebugAssert( !IsHybrid );
        Throw.DebugAssert( suffixLength > 0 );
        SetSpan( _start, _length - suffixLength );
        return CreateSnapshot();
    }

    internal void NextLongestPrefix()
    {
        SetSpan( _start, _length - 1 );
    }

    MutableRPSetKey SetSpan( int start, int length )
    {
        Throw.DebugAssert( start >= _baseStart && start < _baseLength );
        int iMax = start + length;
        Throw.DebugAssert( iMax < _indexes.Length );

        _start = start;
        _length = length;
        HashCode c = new HashCode();
        for( int i = start; i < iMax; ++i )
        {
            c.Add( _indexes[i] );
        }
        _hash = c.ToHashCode();
        return this;
    }

    public int GetSinglePackageIndex()
    {
        Throw.DebugAssert( _length == 1 );
        var i = _indexes[_start];
        return i < 0 ? ~i : i;
    }

    public int Length => _length;

    public bool IsLocalDependent => _indexes[_start] < 0;

    public bool IsHybrid => _indexes[_start] < 0 && _indexes[_start + _length - 1] >= 0;

    public ReadOnlySpan<int> PackageIndexes => _indexes.AsSpan( _start, _length );

    public override bool Equals( object? obj )
    {
        Throw.DebugAssert( "There cannot be 2 MutableRPSetKey at the same time.",
                            obj is not MutableRPSetKey );
        return obj is ORPSetKey k && PackageIndexes.SequenceEqual( k.PackageIndexes );
    }

    public override int GetHashCode() => _hash;

    public readonly ref struct Snapshot
    {
        readonly MutableRPSetKey _k;
        readonly int _hash;
        readonly int _baseStart;
        readonly int _baseLength;
        readonly int _start;
        readonly int _length;

        public Snapshot( MutableRPSetKey k )
        {
            _hash = k._hash;
            _baseStart = k._baseStart;
            _baseLength = k._baseLength;
            _start = k._start;
            _length = k._length;
            _k = k;
        }

        public void Dispose()
        {
            _k._hash = _hash;
            _k._baseStart = _baseStart;
            _k._baseLength = _baseLength;
            _k._start = _start;
            _k._length = _length;

        }
    }

    public Snapshot CreateSnapshot() => new Snapshot( this );

}
