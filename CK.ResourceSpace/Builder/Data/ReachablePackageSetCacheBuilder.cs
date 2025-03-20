using CK.BinarySerialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

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
    readonly List<IReachablePackageSet> _stableSets;
    readonly List<IReachablePackageSet> _localSets;

    public ReachablePackageSetCacheBuilder()
    {
        _index = new Dictionary<object, IReachablePackageSet>();
        _stableSets = new List<IReachablePackageSet>();
        _localSets = new List<IReachablePackageSet>();
    }

    public IReachablePackageSet RegisterEmpty( ResPackage declarer )
    {
        var targetSet = declarer.IsEventuallyLocalDependent
                        ? _localSets
                        : _stableSets;
        var result = new RPEmpty( declarer, targetSet.Count );
        targetSet.Add( result );
        _index.Add( declarer, result );
        return result;
    }

    public IReachablePackageSet RegisterSet( RPSet set )
    {
        Throw.DebugAssert( set.Count > 0 );
        if( set.Count == 1 )
        {
            // A set with a single item is the same as the item in terms
            // of cached/associated information.
            // Note that we have necessarily processed the package already.
            return new RPSingle( (RPEmpty)_index[set.First()] );
        }
        var key = new ORPSetKey( set );
        if( !_index.TryGetValue( key, out var result ) )
        {
            var targetSet = key.IsLocalDependent
                                ? _localSets
                                : _stableSets;
            set.SetCachedIndex( targetSet.Count );
            targetSet.Add( set );
            _index.Add( key, set );
            // Handle the pairs here (we may implement a RDPair once).
            // This relies on the fact that reachable packages have necessarily
            // already been handled: the 2 packages have been registered as RPEmpty.
            if( set.Count == 2 )
            {
                set.SettlePair( _index );
                Throw.DebugAssert( set.IsLocalDependent == key.IsLocalDependent );
            }
            result = set;
        }
        return result;
    }

    public ReachablePackageSetCache Build( IActivityMonitor monitor, ImmutableArray<ResPackage> packages )
    {
        Throw.DebugAssert( "All set indexed by a ORPSetKey are RPSet.",
                           _index.All( kv => kv.Key is not ORPSetKey || kv.Value is RPSet ) );
        Throw.DebugAssert( "We have no RPFake yet.",
                           !_index.Values.Any( set => set is RPFake ) );

        // Consider all the sets with more than 2 packages (pairs have already
        // be bound to their 2 RPEmpty sets) and group them by the number of
        // packages they contain.
        // Using ToLookup here that concretizes the IEnumerable instead of GroupBy
        // since we'll add new RPFake in the index.
        var byLength = _index.Where( kv => kv.Key is ORPSetKey k && k.Length > 2 )
                              .Select( kv => (Key: Unsafe.As<ORPSetKey>(kv.Key), Set: Unsafe.As<RPSet>( kv.Value )) )
                              .ToLookup( kv => kv.Key.Length )
                              .OrderBy( g => g.Key );
        // Reusable mutable key.
        var lookupKey = new MutableRPSetKey();
        foreach( var lengthGroup in byLength )
        {
            int length = lengthGroup.Key;
            foreach( var (key,set) in lengthGroup )
            {
                IReachablePackageSet s1;
                IReachablePackageSet s2;
                if( key.IsHybrid )
                {
                    if( key.IsTrivialLocalHybrid )
                    {
                        Throw.DebugAssert( key.PackageIndexes[0] < 0 );
                        s1 = _index[packages[~key.PackageIndexes[0]]];
                        s2 = FindOrCreate( key.SetTrivialLocalLookup( lookupKey ), packages );
                    }
                    else if( key.IsTrivialStableHybrid)
                    {
                        Throw.DebugAssert( key.PackageIndexes[^1] > 0 );
                        s2 = _index[packages[key.PackageIndexes[^1]]];
                        s1 = FindOrCreate( key.SetTrivialStableLookup( lookupKey ), packages );
                    }
                    else
                    {
                        Throw.DebugAssert( key.Length >= 4 );
                        s1 = FindOrCreate( key.SetHybridLookup( lookupKey ), packages );
                        s2 = FindOrCreate( lookupKey.MoveToStable(), packages );
                    }
                    Throw.DebugAssert( s1.IsLocalDependent && !s2.IsLocalDependent );
                }
                else
                {
                    // Homogeneous case: all packages are either stable or local dependent.
                    (s1, s2) = Ensure( key.SetHomogeneousLookup( lookupKey ), packages );
                }
                set.Settle( s1, s2 );
            }
        }

        return new ReachablePackageSetCache( _stableSets, _localSets );
    }

    IReachablePackageSet FindOrCreate( MutableRPSetKey lookupKey, ImmutableArray<ResPackage> packages )
    {
        Throw.DebugAssert( lookupKey.Length >= 1 && !lookupKey.IsHybrid );
        if( !TryFind( lookupKey, packages, out var set ) )
        {
            Throw.DebugAssert( "We would have fonud a single package.", lookupKey.Length >= 2 );
            var (s1, s2) = Ensure( lookupKey, packages );
            var store = lookupKey.IsLocalDependent
                            ? _localSets
                            : _stableSets;
            set = new RPFake( s1, s2, store.Count );
            store.Add( set );
            _index.Add( new ORPSetKey( lookupKey ), set );
        }
        return set;
    }

    (IReachablePackageSet, IReachablePackageSet) Ensure( MutableRPSetKey lookupKey, ImmutableArray<ResPackage> packages )
    {
        Throw.DebugAssert( lookupKey.Length >= 2 && !lookupKey.IsHybrid );
        Throw.DebugAssert( !_index.ContainsKey( lookupKey ) );
        // When handling local dependent, package indexes are bitwise complemented so
        // the biggest package number (tail packages) appear before the smaller ones (head packages).
        // If we can create binary nodes that roughly follow the topological sort order (biggest
        // package index have less impacts than small ones), we limit the impact of a change
        // in the live state.
        //
        // [<tail packages - less impacts>...<head packages - more impacts>]
        //
        // So here we privilegiate a binary node that cover the longest possible suffix.
        // For stable, we don't care to "revert" these dependencies because the cached information
        // will be created only once, the order doesn't matter.
        //
        // Note that a DAG cannot fit into a tree: the live cache invalidity is based on the dependency
        // graph. Here we try to minimize the number of aggregation required to restore the cache after
        // a change.
        //
        var (suffix, suffixLength) = FindLongestSuffix( lookupKey, packages );
        var (prefix, prefixLength) = FindLongestPrefix( lookupKey, packages, suffixLength );
        Throw.DebugAssert( prefixLength + suffixLength <= lookupKey.Length );
        // We may have a hole to fill.
        // [<prefix>, P1, P2... Pn, <suffix>]
        int holeSize = lookupKey.Length - (prefixLength + suffixLength); 
        if( holeSize == 0 )
        {
            // Happy path!
            return (prefix, suffix);
        }
        // We (recursively) find or create a set for hole = [P1...Pn] and
        // consider our suffix to be a new RPFake( hole, <suffix> ).
        // Can we do better here? Maybe but we don't know.
        // We could consider that creating a [P1,[P2...[Pn,<suffix>]...] will
        // produce suffixes that will be (more) reused whereas the [P1...Pn] here
        // will not (or less). Or maybe we missed a [P1..Pn+1,<shorter suffix without Pn>].
        // We may implement here a deferred resolution (via post actions) that will
        // have all the "holes" to resolve and find the optimal set.
        // This definitly looks like a NP problem...
        // So we stick to this heuristic of (prefix/hole/longest-suffix), at least for now.
        lookupKey.SetHole( prefixLength, holeSize );
        var hole = FindOrCreate( lookupKey, packages );
        var store = lookupKey.IsLocalDependent ? _localSets : _stableSets;
        suffix = new RPFake( hole, suffix, store.Count );
        store.Add( suffix );
        _index.Add( new ORPSetKey( lookupKey ), suffix );
        return (prefix, suffix);
    }

    (IReachablePackageSet prefix, int prefixLength) FindLongestPrefix( MutableRPSetKey lookupKey,
                                                                       ImmutableArray<ResPackage> packages,
                                                                       int suffixLength )
    {
        using var _ = lookupKey.StartLongestPrefix( suffixLength );
        IReachablePackageSet? prefix = null;
        while( !TryFind( lookupKey, packages, out prefix ) )
        {
            lookupKey.NextLongestPrefix();
        }
        Throw.DebugAssert( "We necessarily found the 1-length first RPEmpty package.", prefix != null );
        return (prefix, lookupKey.Length);
    }

    (IReachablePackageSet suffix, int suffixLength) FindLongestSuffix( MutableRPSetKey lookupKey, ImmutableArray<ResPackage> packages )
    {
        using var _ = lookupKey.StartLongestSuffix();
        IReachablePackageSet? suffix = null;
        while( !TryFind( lookupKey, packages, out suffix ) )
        {
            lookupKey.NextLongestSuffix();
        }
        Throw.DebugAssert( "We necessarily found the 1-length last RPEmpty package.", suffix != null );
        return (suffix, lookupKey.Length);
    }

    bool TryFind( MutableRPSetKey lookupKey, ImmutableArray<ResPackage> packages, [NotNullWhen( true )] out IReachablePackageSet? set )
    {
        Throw.DebugAssert( lookupKey.Length >= 1 );
        if( lookupKey.Length == 1 )
        {
            return _index.TryGetValue( packages[lookupKey.GetSinglePackageIndex()], out set );
        }
        return _index.TryGetValue( lookupKey, out set );
    }
}
