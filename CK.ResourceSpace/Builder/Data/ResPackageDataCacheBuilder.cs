using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Core;

sealed class ResPackageDataCacheBuilder
{
    // Contains AggregateKey to index mapping for local and stable.
    // Only AggregateKey for more than one package is computed. The AggregateId
    // (localKeyId and StableKeyId) for single package set is the single package identifier.
    // True AggregateId key (with at least 2 packages) start at the _totalPackageCount.
    readonly Dictionary<AggregateKey, int> _aggregateIndex;
    // Stores for true AggregateId keys.
    readonly List<AggregateKey> _stableAggregates;
    readonly List<AggregateKey> _localAggregates;
    // Reachable packages are deduplicated and shared instances are
    // exposed by ResPackage.ReachablePackages.
    readonly Dictionary<AggregateId, HashSet<ResPackage>> _reachableIndex;
    // Reusable buffers (avoiding allocation).
    readonly int[] _localBuffer;
    readonly int[] _stableBuffer;
    // This considers "<App>" and "<Code>" packages, this is the ResPackageDataCache.DataCacheLength
    // and the offset of true AggregateId keys.
    readonly int _totalPackageCount;
    // Relay to the ResPackage constructor.
    internal readonly Dictionary<IResourceContainer, IResPackageResources> _resourceIndex;

    public ResPackageDataCacheBuilder( int descriptorPackageCount,
                                       int collectorLocalPackageCount,
                                       bool appHasLocalPath,
                                       Dictionary<IResourceContainer, IResPackageResources> resourceIndex )
    {
        _aggregateIndex = new Dictionary<AggregateKey, int>();
        _reachableIndex = new Dictionary<AggregateId, HashSet<ResPackage>>();
        _stableAggregates = new List<AggregateKey>();
        _localAggregates = new List<AggregateKey>();
        // We include the tail "<App>" package here regardless of whether a
        // path for the App has been provided because "<App>" is local dependent...
        // Except if we have no local package at all and no path for the App: in this
        // case we have absolutely NO local package!
        _localBuffer = new int[collectorLocalPackageCount != 0
                                ? collectorLocalPackageCount + 1
                                : appHasLocalPath
                                    ? 1
                                    : 0];
        // We include the head "<Code>" package here.
        _stableBuffer = new int[1 + descriptorPackageCount - collectorLocalPackageCount];
        // Consider "<App>" and "<Code>" packages. 
        _totalPackageCount = collectorLocalPackageCount + 2;
        _resourceIndex = resourceIndex;
    }

    public int TotalPackageCount => _totalPackageCount;

    public IReadOnlySet<ResPackage> GetClosure( IReadOnlyCollection<ResPackage> packages, out AggregateId aggregateId )
    {
        Throw.DebugAssert( packages.Count > 0 );
        aggregateId = RegisterAggregate( packages );
        if( !_reachableIndex.TryGetValue( aggregateId, out var exists ) )
        {
            var closure = new HashSet<ResPackage>();
            foreach( var p in packages )
            {
                if( closure.Add( p ) )
                {
                    closure.UnionWith( p.AfterReachables );
                }
            }
            exists = closure;
            _reachableIndex.Add( aggregateId, exists );
        }
        return exists;
    }

    public AggregateId RegisterAggregate( IReadOnlyCollection<ResPackage> packages )
    {
        int nbLocal = 0;
        int nbStable = 0;
        foreach( var p in packages )
        {
            if( p.IsEventuallyLocalDependent )
            {
                _localBuffer[nbLocal++] = p.Index;
            }
            else
            {
                _stableBuffer[nbStable++] = p.Index;
            }
        }
        var stableAggregateId = nbStable switch
        {
            0 => 0,
            1 => _stableBuffer[0],
            _ => FindOrCreateStable( nbStable ),
        };
        var localAggregateId = nbLocal switch
        {
            0 => 0,
            1 => _localBuffer[0],
            _ => FindOrCreateLocal( nbLocal ),
        };
        return new AggregateId( localAggregateId, stableAggregateId );
    }

    int FindOrCreateStable( int nbStable )
    {
        var key = new AggregateKey( _stableBuffer.AsSpan( 0, nbStable ) );
        if( !_aggregateIndex.TryGetValue( key, out var id ) )
        {
            id = ~(_totalPackageCount + _stableAggregates.Count);
            _stableAggregates.Add( key );
            _aggregateIndex.Add( key, id );
        }
        return id;
    }

    int FindOrCreateLocal( int nbLocal )
    {
        var key = new AggregateKey( _localBuffer.AsSpan( 0, nbLocal ) );
        if( !_aggregateIndex.TryGetValue( key, out var id ) )
        {
            id = _totalPackageCount + _localAggregates.Count;
            _localAggregates.Add( key );
            _aggregateIndex.Add( key, id );
        }
        return id;
    }

    public ResPackageDataCache Build( IActivityMonitor monitor, ImmutableArray<ResPackage> packages )
    {
        return new ResPackageDataCache( _totalPackageCount, packages, _localAggregates, _stableAggregates );
    }
}
