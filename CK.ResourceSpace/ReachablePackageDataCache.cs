using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Core;

/// <summary>
/// Cache for associated information that can be initialized from a single <see cref="ResPackage"/>
/// and computed by aggregation for a set of <see cref="ResPackage"/>.
/// </summary>
/// <typeparam name="T">
/// Associated data type. It should be immutable.
/// </typeparam>
public abstract class ReachablePackageDataCache<T> where T : class
{
    readonly ReachablePackageSetCache _cache;
    readonly T?[] _stableData;
    readonly T?[] _localData;

    /// <summary>
    /// Initializes a new ReachablePackageDataCache.
    /// </summary>
    /// <param name="cache">The central cache.</param>
    protected ReachablePackageDataCache( ReachablePackageSetCache cache )
    {
        _cache = cache;
        _stableData = new T[cache.StableCacheLength];
        _localData = new T[cache.LocalCacheLength];
    }

    /// <summary>
    /// Gets the data associated to a <see cref="IReachablePackageSet"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="set">The reachable package set for wich a <typeparamref name="T"/> must be obtained.</param>
    /// <returns>The data on success, false on error.</returns>
    public T? Get( IActivityMonitor monitor, IReachablePackageSet set )
    {
        return DoGet( monitor, (IRPInternal)set );
    }

    T? DoGet( IActivityMonitor monitor, IRPInternal set )
    {
        var store = set.IsLocalDependent ? _localData : _stableData;
        var r = store[set.CacheIndex];
        if( r != null ) return r;

        T? result;
        if( set is IRPRoot root )
        {
            result = Create( monitor, root.RootPackage );
        }
        else
        {
            Throw.DebugAssert( set is IRPDerived );
            result = Compute( monitor, Unsafe.As<IRPDerived>( set ) );
        }
        store[set.CacheIndex] = result;
        return result;
    }

    T? Compute( IActivityMonitor monitor, IRPDerived agg )
    {
        IRPInternal s1 = _cache.Resolve( agg.CIndex1 );
        var t1 = DoGet( monitor, s1 );
        if( t1 == null ) return null;
        IRPInternal s2 = _cache.Resolve( agg.CIndex2 );
        var t2 = DoGet( monitor, s2 );
        if( t2 == null ) return null;
        return Aggregate( t1, t2 );
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
    /// Aggregates two associated data into one. This cannot fail: if the aggregation is somehow invalid,
    /// this must appear in the <typeparamref name="T"/>.
    /// </summary>
    /// <param name="data1">The first data to aggregate.</param>
    /// <param name="data2">The second data to aggregate.</param>
    /// <returns>Aggregated data. Can be <paramref name="data1"/> or <paramref name="data2"/>.</returns>
    protected abstract T Aggregate( T data1, T data2 );

}
