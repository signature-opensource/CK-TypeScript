using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace CK.Core;

/// <summary>
/// SpaceDataCache implementation in the Live world.
/// </summary>
sealed class LiveSpaceDataCache : IInternalSpaceDataCache
{
    readonly int _dataCacheLength;
    readonly ImmutableArray<ResPackage> _packages;
    readonly ImmutableArray<ImmutableArray<int>> _localAggregates;
    readonly ImmutableArray<ImmutableArray<int>> _stableAggregates;
    readonly int[] _stableIdentifiers;

    LiveSpaceDataCache( ImmutableArray<ResPackage> packages,
                        int dataCacheLength,
                        ImmutableArray<ImmutableArray<int>> localAggregates,
                        ImmutableArray<ImmutableArray<int>> stableAggregates,
                        int[] stableIdentifiers )
    {
        _dataCacheLength = dataCacheLength;
        _packages = packages;
        _localAggregates = localAggregates;
        _stableAggregates = stableAggregates;
        _stableIdentifiers = stableIdentifiers;
    }

    public static LiveSpaceDataCache Read( ICKBinaryReader r, ImmutableArray<ResPackage> packages )
    {
        int dataCacheLength = r.ReadNonNegativeSmallInt32();
        var local = ReadAggregateKeys( r );
        var stable = ReadAggregateKeys( r );

        int[] stableIdentifiers = new int[r.ReadNonNegativeSmallInt32()];
        for( int i = 0; i < stableIdentifiers.Length; ++i )
        {
            stableIdentifiers[i] = r.ReadInt32();
        }
        return new LiveSpaceDataCache( packages, dataCacheLength, local, stable, stableIdentifiers );

        static ImmutableArray<ImmutableArray<int>> ReadAggregateKeys( ICKBinaryReader r )
        {
            var keys = new ImmutableArray<int>[r.ReadNonNegativeSmallInt32()];
            for( int i = 0; i < keys.Length; ++i )
            {
                var indexes = new int[r.ReadNonNegativeSmallInt32()];
                for( int j = 0; j < indexes.Length; ++j )
                {
                    indexes[j] = r.ReadInt32();
                }
                keys[i] = ImmutableCollectionsMarshal.AsImmutableArray( indexes );
            }
            return ImmutableCollectionsMarshal.AsImmutableArray( keys );
        }
    }

    void ISpaceDataCache.LocalImplementationOnly() { }

    public int DataCacheLength => _dataCacheLength;

    public int StableAggregateCacheLength => _stableAggregates.Length;

    public int LocalAggregateCacheLength => _localAggregates.Length;

    public ImmutableArray<ResPackage> Packages => _packages;

    public IReadOnlyCollection<int> StableIdentifiers => _stableIdentifiers;

    public ReadOnlySpan<int> GetStableAggregate( int trueAggregateId )
    {
        Throw.DebugAssert( trueAggregateId >= 0 && trueAggregateId < _stableAggregates.Length );
        return _stableAggregates[trueAggregateId].AsSpan();
    }

    public ReadOnlySpan<int> GetLocalAggregate( int trueAggregateId )
    {
        Throw.DebugAssert( trueAggregateId >= 0 && trueAggregateId < _localAggregates.Length );
        return _localAggregates[trueAggregateId].AsSpan();
    }

}
