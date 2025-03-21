using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Root ResPackageDataCache implementation in engine world.
/// </summary>
sealed class ResPackageDataCache : IInternalResPackageDataCache
{
    readonly int _dataCacheLength;
    readonly ImmutableArray<ResPackage> _packages;
    readonly List<AggregateKey> _localAggregates;
    readonly List<AggregateKey> _stableAggregates;

    internal ResPackageDataCache( int dataCacheLength,
                                  ImmutableArray<ResPackage> packages,
                                  List<AggregateKey> localAggregates,
                                  List<AggregateKey> stableAggregates )
    {
        _dataCacheLength = dataCacheLength;
        _packages = packages;
        _localAggregates = localAggregates;
        _stableAggregates = stableAggregates;
    }

    void IResPackageDataCache.LocalOnly() { }

    public int DataCacheLength => _dataCacheLength;

    public int StableAggregateCacheLength => _stableAggregates.Count;

    public int LocalAggregateCacheLength => _localAggregates.Count;

    public ImmutableArray<ResPackage> ZeroBasedPackages => _packages;

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
