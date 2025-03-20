using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Cache of <see cref="IReachablePackageSet"/>. This is an opaque object
/// that supports <see cref="ReachablePackageDataCache{T}"/> machinery.
/// </summary>
public sealed class ReachablePackageSetCache
{
    readonly List<IRPInternal> _stableSets;
    readonly List<IRPInternal> _localSets;

    internal ReachablePackageSetCache( List<IRPInternal> stableSets, List<IRPInternal> localSets )
    {
        _stableSets = stableSets;
        _localSets = localSets;
    }

    internal int StableCacheLength => _stableSets.Count;

    internal int LocalCacheLength => _localSets.Count;

    internal IRPInternal Resolve( int cIndex )
    {
        return cIndex < 0 ? _localSets[~cIndex] : _stableSets[cIndex];
    }
}
