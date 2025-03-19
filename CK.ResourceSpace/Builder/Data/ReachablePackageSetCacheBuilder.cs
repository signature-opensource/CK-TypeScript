using CK.BinarySerialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Implements a pool of HashSet of ResPackage. This is used ony for <see cref="ResPackage.ReachablePackages"/>
/// and <see cref="ResPackage.AfterReachablePackages"/>, not for their respective transitive closures.
/// <para>
/// This also implements the "optimal aggregation set". The optimal aggregation set for a set of package
/// cannot be computed when pooling a new set: even if packages before/after are processed in the topological
/// order, a better combination may appear afterwards. Example:
/// A pools [B,C]        => no optimal (optimal is [B,C]).
/// D pools [B,C]        => shared with A.
/// E pools [B,C,X,Y,Z]  => the optimal aggregate so far is [A,X,Y,Z].
/// F pools [B,C,X,Y]    => the optimal aggregate so far is [A,X,Y]
///                         ...BUT the optimal for E becomes [F,Z]
/// 
/// This is because the "optimal aggregate" doesn't respect the topological order: it is meant to be used
/// by creating an aggregated derived information on demand and cache it: it is the creation itself that
/// follows the topological order and fills the holes in the aggregate cache as needed, regardless of the packages order.
/// </para>
/// </summary>
sealed class ReachablePackageSetCacheBuilder
{
    // Index the sets by RootPackage when the sets is empty (RPEmpty)
    // or by ReachablePackageSetKey (RPSet).
    // Sets that contain only one package (RPSingle) are not indexed: they are
    // wrappers on the RPEmpty of their single package.
    readonly Dictionary<object, IReachablePackageSet> _index;
    readonly List<IReachablePackageSet> _sets;

    public ReachablePackageSetCacheBuilder()
    {
        _index = new Dictionary<object, IReachablePackageSet>();
        _sets = new List<IReachablePackageSet>();
    }

    public IReachablePackageSet RegisterEmpty( ResPackage declarer )
    {
        var result = new RPEmpty( declarer, _sets.Count );
        _sets.Add( result );
        _index.Add( declarer, result );
        return result;
    }

    public IReachablePackageSet RegisterSet( RPSet set )
    {
        Throw.DebugAssert( set.Count > 0 );
        if( set.Count == 1 )
        {
            // Here is the first step of the "optimal aggregate set": a set with
            // a single item is the same as the item in terms of cached/associated information.
            // Note that we have necessarily processed the package already.
            return new RPSingle( (RPEmpty)_index[set.First()] );
        }
        var key = new ORPSetKey( set );
        if( !_index.TryGetValue( key, out var result ) )
        {
            // Handle the pairs here (we may implement a RDPair once).
            // This relies on the fact that reachable packages have necessarily
            // already been handled: the 2 packages have been registered as RPEmpty.
            if( set.Count == 2 )
            {
                set.InitializePair( _index );
            }
            result = set;
            set._index = _sets.Count;
            _sets.Add( result );
            _index.Add( key, result );
        }
        return result;
    }

    public ReachablePackageSetCache Build( IActivityMonitor monitor, ImmutableArray<ResPackage> packages )
    {
        // This is the array of "real" sets, the ones that do exist.
        // New "virtual" ones will have index greater or equal to
        // the length of this array.
        var all = _sets.ToImmutableArray();

        // Consider all the sets with more than 2 packages (pairs have already
        // be bound to their 2 RPEmpty set) and group them by the number of
        // packages they contain.
        // Using ToLookup here that concretizes the set instead of GroupBy
        // since we'll add new RPFake in the index.
        var byLength = _index.Keys.OfType<ORPSetKey>()
                                  .Where( k => k.Length > 2 )
                                  .ToLookup( k => k.Length )
                                  .OrderBy( g => g.Key );
        // Reusable Mutable key.
        var lookupKey = new MutableRPSetKey();
        foreach( var lengthGroup in byLength )
        {
            int length = lengthGroup.Key;
            foreach( var key in lengthGroup )
            {
                key.ResetMutable( lookupKey );
                var (prefix, remainder) = GetLongestPrefix( lookupKey, packages, _index );
                if( remainder)
            }
        }
    }

    static (IReachablePackageSet, IReachablePackageSet) GetLongestPrefix( MutableRPSetKey lookupKey,
                                                                          ImmutableArray<ResPackage> packages,
                                                                          Dictionary<object, IReachablePackageSet> index )
    {
        // Privilegiates the longest prefix.
        for( int len = lookupKey.Length-1; len > 0; --len )
        {
            lookupKey.SetSpan( 0, len );
            if( TryFind( lookupKey, packages, index, out var prefix ) )
            {
                lookupKey.SetSpan( lookupKey.Length - len, len );
                if( TryFind( lookupKey, packages, index, out var suffix ) )
                {
                    // Happy path!
                    return (prefix, suffix);
                }
                // The suffix doesn't exist.
                
            }
        }
        // Never reached.
        throw new CKException( "Whe should have found the single RPEmpty set for the first package index." );
    }

    static bool TryFind( MutableRPSetKey lookupKey,
                         ImmutableArray<ResPackage> packages,
                         Dictionary<object, IReachablePackageSet> index,
                         [NotNullWhen(true)] out IReachablePackageSet? set )
    {
        if( lookupKey.Length == 1 )
        {
            return index.TryGetValue( packages[lookupKey.GetSinglePackageIndex()], out set );
        }
        return index.TryGetValue( lookupKey, out set );
    }
}

/// <summary>
/// Cache for associated information that can be initialized from a single <see cref="ResPackage"/>
/// and computed by aggregation for a set of <see cref="ResPackage"/>.
/// </summary>
/// <typeparam name="T">
/// Associated data type. It must be immutable.
/// </typeparam>
public abstract class ReachablePackageDictionary<T> where T : class
{
    readonly ReachablePackageSetCache _cache;
    readonly T?[] _stableData;
    readonly T?[] _localData;

    protected ReachablePackageDictionary( ReachablePackageSetCache cache )
    {
        _cache = cache;
        _stableData = new T[cache.StableCacheLength];
    }

    public T? Get( IActivityMonitor monitor, IReachablePackageSet set )
    {
        var store = set.IsLocalDependent ? _localData : _stableData;
        var r = store[set.Index];
        if( r != null ) return r;
        if( set is IRPRoot root )
        {
            var result = Create( monitor, root.RootPackage );
            store[set.Index] = result;
            return result;
        }
        else
        {
            Throw.DebugAssert( set.Count >= 2 );
            var e = set.GetEnumerator();
            e.MoveNext();
            var d1 = e.Current;
            e.MoveNext();
            var d2 = e.Current;
            var result = Aggregate( )

        }
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
