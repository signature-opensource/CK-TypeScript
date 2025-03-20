using System;
using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Reachable packages exposed by <see cref="ResPackage.ReachablePackages"/> and <see cref="ResPackage.AfterReachablePackages"/>.
/// </summary>
public interface IReachablePackageSet : IReadOnlySet<ResPackage>
{
    /// <summary>
    /// Gets a unique index for this set of packages.
    /// It is the index of this set in the <see cref="ReachablePackageSetCache.All"/>.
    /// </summary>
    int CacheIndex { get; }

    /// <summary>
    /// Gets whether this reachable set is local dependent.
    /// At least one of its packages has a true <see cref="ResPackage.IsEventuallyLocalDependent"/>.
    /// </summary>
    bool IsLocalDependent { get; }
}
