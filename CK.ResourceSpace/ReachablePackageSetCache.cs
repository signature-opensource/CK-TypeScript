using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Cache of <see cref="IReachablePackageSet"/>.
/// </summary>
public sealed class ReachablePackageSetCache
{
    readonly ImmutableArray<RPSet> _sets;

    /// <summary>
    /// Gets all the <see cref="IReachablePackageSet"/> indexed by their <see cref="IReachablePackageSet.Index"/>.
    /// </summary>
    public ImmutableArray<IReachablePackageSet> All => ImmutableArray<IReachablePackageSet>.CastUp( _sets );
}
