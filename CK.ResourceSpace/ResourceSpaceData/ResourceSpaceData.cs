using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// <see cref="ResourceSpace"/> core data is produced by the <see cref="ResourceSpaceDataBuilder"/>.
/// </summary>
public sealed class ResourceSpaceData
{
    readonly IReadOnlyDictionary<object, ResPackage> _packageIndex;
    internal ImmutableArray<ResPackage> _packages;

    internal ResourceSpaceData( IReadOnlyDictionary<object, ResPackage> packageIndex )
    {
        _packageIndex = packageIndex;
    }

    /// <summary>
    /// Gets the packages indexed by their <see cref="ResPackage.FullName"/>, <see cref="ResPackage.Type"/> (if
    /// the package is defined by a type), <see cref="ResPackage.PackageResources"/> and <see cref="ResPackage.CodeGenResources"/>.
    /// </summary>
    public IReadOnlyDictionary<object, ResPackage> PackageIndex => _packageIndex;

    /// <summary>
    /// Gets the packages topologically ordered. <see cref="ResPackage.Index"/> is the index in this array.
    /// </summary>
    public ImmutableArray<ResPackage> Packages => _packages;
}
