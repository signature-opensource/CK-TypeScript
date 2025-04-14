using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

sealed class SpaceDataCacheBuilder
{
    readonly ResSpaceData _spaceData;
    // Contains AggregateKey to index mapping for local and stable.
    // Only AggregateKey for more than one package is computed. The AggregateId
    // (localKeyId and StableKeyId) for single package set is the single package identifier.
    // True AggregateId key (with at least 2 packages) start at the _totalPackageCount.
    readonly Dictionary<AggregateKey, int> _aggregateIndex;
    // Stores for true AggregateId keys.
    readonly List<AggregateKey> _stableAggregates;
    readonly List<AggregateKey> _localAggregates;
    // Reachable packages are deduplicated and shared instances are
    // exposed by ResPackage.Reachables.
    readonly Dictionary<AggregateId, HashSet<ResPackage>> _reachableIndex;
    // Reusable buffers (avoiding allocation).
    readonly int[] _localBuffer;
    readonly int[] _stableBuffer;
    // This considers "<App>" and "<Code>" packages, this is the ResPackageDataCache.DataCacheLength
    // and the offset of true AggregateId keys.
    readonly int _totalPackageCount;

    public SpaceDataCacheBuilder( ResSpaceData spaceData,
                                  int descriptorPackageCount,
                                  int collectorLocalPackageCount,
                                  bool appHasLocalPath )
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
        _totalPackageCount = descriptorPackageCount + 2;
        _spaceData = spaceData;
    }

    public int TotalPackageCount => _totalPackageCount;

    public ResSpaceData SpaceData => _spaceData;

    public IReadOnlySet<ResPackage> GetReachableClosure( IReadOnlyCollection<ResPackage> packages, out AggregateId aggregateId )
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
        var localAggregateId = nbLocal switch
        {
            0 => 0,
            1 => _localBuffer[0] + 1,
            _ => FindOrCreateLocal( nbLocal ),
        };
        var stableAggregateId = nbStable switch
        {
            0 => 0,
            1 => _stableBuffer[0] + 1,
            _ => FindOrCreateStable( nbStable ),
        };
        return new AggregateId( localAggregateId, stableAggregateId );
    }

    int FindOrCreateStable( int nbStable )
    {
        var key = new AggregateKey( _stableBuffer.AsSpan( 0, nbStable ) );
        if( !_aggregateIndex.TryGetValue( key, out var id ) )
        {
            // Add first: offsets the id by 1.
            _stableAggregates.Add( key );
            id = _totalPackageCount + _stableAggregates.Count;
            _aggregateIndex.Add( key, id );
        }
        return id;
    }

    int FindOrCreateLocal( int nbLocal )
    {
        var key = new AggregateKey( _localBuffer.AsSpan( 0, nbLocal ) );
        if( !_aggregateIndex.TryGetValue( key, out var id ) )
        {
            _localAggregates.Add( key );
            id = _totalPackageCount + _localAggregates.Count;
            _aggregateIndex.Add( key, id );
        }
        return id;
    }

    public SpaceDataCache Build( IActivityMonitor monitor, ImmutableArray<ResPackage> packages, bool withLiveState )
    {
        Throw.DebugAssert( _totalPackageCount == packages.Length );
        // Computes the stable identifiers.
        HashSet<int>? stableIdentifiers = null;
        if( withLiveState )
        {
            // The <App> may not be IsLocalPackage but if we are here (because there's a watch root), then
            // there's at least one local package and the <App> is necessarily local dependent.
            Throw.DebugAssert( _spaceData.AppPackage.IsEventuallyLocalDependent );
            stableIdentifiers = new HashSet<int>();
            foreach( var p in _spaceData.LocalPackages )
            {
                Throw.DebugAssert( p.IsLocalPackage );
                var (requiresAggregateId, childrenAggregateId) = p.GetAggregateIdentifiers();
                // This may be a single package identifier (offset by 1)
                // or a aggregate identifier (greater than total package count).
                if( requiresAggregateId.HasStable )
                {
                    stableIdentifiers.Add( requiresAggregateId._stableKeyId );
                }
                if( childrenAggregateId.HasStable )
                {
                    stableIdentifiers.Add( childrenAggregateId._stableKeyId );
                }
            }
            monitor.Debug( $"Optimal Stable Aggregated Data set has {stableIdentifiers.Count} cache entries for {_spaceData.LocalPackages.Length} local data." );
        }
        return new SpaceDataCache( packages, _localAggregates, _stableAggregates, stableIdentifiers );
    }
}
