using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Core;

/// <summary>
/// Template method pattern that manage cached information associated a <see cref="ResPackage"/>.
/// </summary>
/// <typeparam name="T">
/// Associated data type. It should be immutable and is fully "implemented" by the
/// <see cref="Create(CK.Core.IActivityMonitor, CK.Core.ResPackage)"/>, <see cref="Combine(CK.Core.IActivityMonitor, CK.Core.IResPackageResources, T)"/>
/// and <see cref="Aggregate(T, T)"/>.
/// </typeparam>
public abstract class ResPackageDataHandler<T> where T : class
{
    readonly IInternalResPackageDataCache _cache;
    readonly T?[] _data;
    readonly T?[] _stableAggregateCache;
    readonly T?[] _localAggregateCache;

    /// <summary>
    /// Initializes a new ReachablePackageDataCache.
    /// </summary>
    /// <param name="cache">The required cache provided by the <see cref="ResourceSpaceData.ResPackageDataCache"/>.</param>
    protected ResPackageDataHandler( IResPackageDataCache cache )
    {
        var c = (IInternalResPackageDataCache)cache;
        _cache = c;
        _data = new T[c.DataCacheLength];
        _stableAggregateCache = new T[c.StableAggregateCacheLength];
        _localAggregateCache = new T[c.LocalAggregateCacheLength];
    }

    /// <summary>
    /// Gets the cache instance to which this data handler is bound.
    /// </summary>
    public IResPackageDataCache ResPackageDataCache => _cache;

    /// <summary>
    /// Gets the data associated to a <see cref="ResPackage"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="package">The package for wich a <typeparamref name="T"/> must be obtained.</param>
    /// <returns>The data on success, null on error.</returns>
    public T? Obtain( IActivityMonitor monitor, ResPackage package )
    {
        var d = _data[package.Index];
        if( d == null )
        {
            d = Build( monitor, package );
            _data[package.Index] = d;
        }
        return d;
    }

    T? Build( IActivityMonitor monitor, ResPackage package )
    {
        T? result = null;
        var (reachableAggregateId, childrenAggregateId) = package.GetAggregateIdentifiers();
        if( reachableAggregateId == default )
        {
            result = Create( monitor, package );
        }
        else
        {
            var before = GetAggregate( monitor, reachableAggregateId );
            if( before != null )
            {
                result = Combine( monitor, package.Resources, before );
            }
        }
        if( result != null && package.Children.Length > 0 )
        {
            var content = GetAggregate( monitor, childrenAggregateId );
            result = content != null
                        ? Aggregate( result, content )
                        : null;
        }
        if( result != null )
        {
            result = Combine( monitor, package.ResourcesAfter, result );
        }
        return result;
    }

    T? GetAggregate( IActivityMonitor monitor, AggregateId id )
    {
        if( !TryGetAggregate( monitor, _stableAggregateCache, id._stableKeyId, out var stable ) 
            || !TryGetAggregate( monitor, _localAggregateCache, id._localKeyId, out var local ) )
        {
            return null;
        }
        return stable == null
                ? local
                : local == null
                    ? stable
                    : Aggregate( local, stable );
                    
    }

    T? Obtain( IActivityMonitor monitor, int packageIndex )
    {
        var d = _data[packageIndex];
        if( d == null )
        {
            d = Build( monitor, _cache.ZeroBasedPackages[packageIndex] );
            _data[packageIndex] = d;
        }
        return d;
    }

    bool TryGetAggregate( IActivityMonitor monitor, T?[] aggregateCache, int keyId, out T? data )
    {
        if( keyId == 0 )
        {
            data = null;
            return true;
        }
        int trueAggId = keyId - _data.Length;
        if( trueAggId < 0 )
        {
            data = Obtain( monitor, keyId );
            return data != null;
        }
        data = aggregateCache[trueAggId];
        if( data == null )
        {
            var packageIdentifiers = aggregateCache == _stableAggregateCache
                                        ? _cache.GetStableAggregate( trueAggId )
                                        : _cache.GetLocalAggregate( trueAggId );
            Throw.DebugAssert( packageIdentifiers.Length >= 2 );
            var e = packageIdentifiers.GetEnumerator();
            e.MoveNext();
            data = Obtain( monitor, e.Current );
            if( data == null ) return false;
            while( e.MoveNext() )
            {
                var next = Obtain( monitor, e.Current );
                if( next == null ) return false;
                data = Aggregate( data, next );
            }
            aggregateCache[trueAggId] = data;
        }
        return true;
    }

    /// <summary>
    /// Initializer function called for packages that have no <see cref="ResPackage.ReachablePackages"/>.
    /// Returning null indicates a failure, errors must have been logged.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="package">The package without dependency for which a <typeparamref name="T"/> must be created.</param>
    /// <returns>The associated data or null on error.</returns>
    protected abstract T? Create( IActivityMonitor monitor, ResPackage package );

    /// <summary>
    /// Applies whatever contains a <see cref="IResPackageResources"/> to an existing data.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="resources">The package's resources to consider.</param>
    /// <param name="data">The data initial data to consider.</param>
    /// <returns>The associated data or null on error.</returns>
    protected abstract T? Combine( IActivityMonitor monitor, IResPackageResources resources, T data );

    /// <summary>
    /// Aggregates two associated data into one. This cannot fail: if the aggregation is somehow invalid,
    /// this must appear in the <typeparamref name="T"/>.
    /// </summary>
    /// <param name="data1">The first data to aggregate.</param>
    /// <param name="data2">The second data to aggregate.</param>
    /// <returns>Aggregated data. Can be <paramref name="data1"/> or <paramref name="data2"/>.</returns>
    protected abstract T Aggregate( T data1, T data2 );

}
