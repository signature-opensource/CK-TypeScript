using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Implements a pool of HashSet of ResPackage. This is used ony for <see cref="ResPackage.ReachablePackages"/>
/// and <see cref="ResPackage.AfterReachablePackages"/>, not for their respective transitive closures.
/// <para>
/// This also implements the "optimal aggregation set". The optimal aggregation set for a set of package
/// cannot be computed when pooling a new set: even if packages before/after are processed in the topological
/// order, a better combination may appear afterwards. Example:
/// A pools [B,C]        => no optimal (optimal is [B,C]).
/// D pools [B,C]        => the optimal aggregate is the aggregate of A: [A].
/// E pools [B,C,X,Y,Z]  => the optimal aggregate so far is [A,X,Y,Z].
/// F pools [B,C,X,Y]    => the optimal aggregate so far is [A,X,Y] BUT the optimal for E becomes [F,Z]
/// 
/// This is because the "optimal aggregate" doesn't respect the topological order: it is meant to be used
/// by creating an aggregated derived information on demand and cache it: it is the creation itself that
/// follows the topological order and fills the holes in the aggregate cache as needed, regardless of the packages order.
/// </para>
/// </summary>
sealed class ReachablePackageCacheBuilder
{
    readonly Dictionary<ReachablePackageSetKey, (int Index, HashSet<ResPackage> Set)> _sets;

    public ReachablePackageCacheBuilder()
    {
        _sets = [];
    }

    public HashSet<ResPackage> Pool( HashSet<ResPackage> set )
    {
        var key = new ReachablePackageSetKey( set );
        if( _sets.TryGetValue( key, out var exists ) )
        {
            return exists.Set;
        }
        _sets.Add( key, (_sets.Count, set) );
        return set;
    }
}
