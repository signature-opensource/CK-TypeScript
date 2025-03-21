using System;
using System.Collections.Immutable;

namespace CK.Core;

interface IInternalResPackageDataCache : IResPackageDataCache
{
    int DataCacheLength { get; }
    int StableAggregateCacheLength { get; }
    int LocalAggregateCacheLength { get; }
    ImmutableArray<ResPackage> ZeroBasedPackages { get; }
    ReadOnlySpan<int> GetStableAggregate( int trueAggregateId );
    ReadOnlySpan<int> GetLocalAggregate( int trueAggregateId );
}
