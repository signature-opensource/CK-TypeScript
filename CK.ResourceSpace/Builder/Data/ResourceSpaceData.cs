using CK.EmbeddedResources;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core;

/// <summary>
/// <see cref="ResourceSpace"/> core data is produced by the <see cref="ResourceSpaceDataBuilder"/>:
/// this contains the final <see cref="ResPackage"/> topologically ordered.
/// <para>
/// This can now be consumed by a <see cref="ResourceSpaceBuilder"/> that can be configured with resource handlers
/// to eventually produce a <see cref="ResourceSpace"/>.
/// </para>
/// </summary>
public sealed class ResourceSpaceData
{
    readonly IReadOnlyDictionary<object, ResPackage> _packageIndex;

    // _packages, _localPackages, _allPackageResources, _codePackage
    // and _appPackage are set by the ResourceSpaceDataBuilder.Build method.
    internal ImmutableArray<ResPackage> _packages;
    internal ImmutableArray<ResPackage> _localPackages;
    internal ImmutableArray<IResPackageResources> _allPackageResources;
    [AllowNull]internal ResPackage _codePackage;
    [AllowNull]internal ResPackage _appPackage;

    internal ResourceSpaceData( IReadOnlyDictionary<object, ResPackage> packageIndex )
    {
        _packageIndex = packageIndex;
    }

    /// <summary>
    /// Gets the packages indexed by their <see cref="ResPackage.FullName"/>, <see cref="ResPackage.Type"/> (if
    /// the package is defined by a type), and by the <see cref="CodeStoreResources.Code"/> and <see cref="CodeStoreResources.Store"/>
    /// resource containers of <see cref="ResPackage.BeforeResources"/> and <see cref="ResPackage.AfterResources"/>.
    /// </summary>
    public IReadOnlyDictionary<object, ResPackage> PackageIndex => _packageIndex;

    /// <summary>
    /// Gets the "&lt;Code&gt;" special head package.
    /// </summary>
    public ResPackage CodePackage => _codePackage;

    /// <summary>
    /// Gets the packages topologically ordered. <see cref="ResPackage.Index"/> is the index in this array.
    /// <para>
    /// This first package is <see cref="CodePackage"/> and the last one is <see cref="AppPackage"/>.
    /// </para>
    /// </summary>
    public ImmutableArray<ResPackage> Packages => _packages;

    /// <summary>
    /// Gets the "&lt;App&gt;" special tail package.
    /// </summary>
    public ResPackage AppPackage => _appPackage;

    /// <summary>
    /// Gets the local packages.
    /// </summary>
    public ImmutableArray<ResPackage> LocalPackages => _localPackages;

    /// <summary>
    /// Gets all the topologically ordered <see cref="IResPackageResources"/> indexed
    /// by their <see cref="IResPackageResources.Index"/>.
    /// </summary>
    public ImmutableArray<IResPackageResources> AllPackageResources => _allPackageResources;
}
