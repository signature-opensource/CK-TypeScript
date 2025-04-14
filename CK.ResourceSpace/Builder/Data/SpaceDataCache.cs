using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using static CK.Core.SpaceDataCache;

namespace CK.Core;

/// <summary>
/// Root SpaceDataCache implementation in engine world.
/// </summary>
sealed class SpaceDataCache : IInternalSpaceDataCache
{
    readonly ImmutableArray<ResPackage> _packages;
    readonly List<AggregateKey> _localAggregates;
    readonly List<AggregateKey> _stableAggregates;
    readonly IReadOnlyCollection<int> _stableIdentifiers;
    readonly int _dataCacheLength;

    internal SpaceDataCache( ImmutableArray<ResPackage> packages,
                             List<AggregateKey> localAggregates,
                             List<AggregateKey> stableAggregates,
                             IReadOnlyCollection<int>? stableIdentifiers )
    {
        _dataCacheLength = packages.Length - 1;
        _packages = packages;
        _localAggregates = localAggregates;
        _stableAggregates = stableAggregates;
        _stableIdentifiers = stableIdentifiers ?? ImmutableArray<int>.Empty;
    }

    void ISpaceDataCache.LocalImplementationOnly() { }

    public void Write( ICKBinaryWriter w )
    {
        w.WriteNonNegativeSmallInt32( _dataCacheLength );
        // AggregateKeys are not deserialized as AggregateKey but
        // only as the array of their PackageIndexes (the hash code is skipped).
        WriteAggregateKeys( w, _localAggregates );
        WriteAggregateKeys( w, _stableAggregates );

        w.WriteNonNegativeSmallInt32( _stableIdentifiers.Count );
        foreach( var id in _stableIdentifiers ) w.Write( id );

        static void WriteAggregateKeys( ICKBinaryWriter w, List<AggregateKey> aggregateKeys )
        {
            w.WriteNonNegativeSmallInt32( aggregateKeys.Count );
            foreach( var k in aggregateKeys )
            {
                var indexes = k.PackageIndexes;
                w.WriteNonNegativeSmallInt32( indexes.Length );
                foreach( var i in indexes )
                {
                    w.Write( i );
                }
            }
        }
    }

    public int DataCacheLength => _dataCacheLength;

    public int StableAggregateCacheLength => _stableAggregates.Count;

    public int LocalAggregateCacheLength => _localAggregates.Count;

    public ImmutableArray<ResPackage> Packages => _packages;

    public IReadOnlyCollection<int> StableIdentifiers => _stableIdentifiers;

    public ReadOnlySpan<int> GetStableAggregate( int trueAggregateId )
    {
        Throw.DebugAssert( trueAggregateId >= 0 && trueAggregateId < _stableAggregates.Count );
        return _stableAggregates[trueAggregateId].PackageIndexes;
    }

    public ReadOnlySpan<int> GetLocalAggregate( int trueAggregateId )
    {
        Throw.DebugAssert( trueAggregateId >= 0 && trueAggregateId < _localAggregates.Count );
        return _localAggregates[trueAggregateId].PackageIndexes;
    }

}
