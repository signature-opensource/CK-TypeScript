using CK.BinarySerialization;
using CK.EmbeddedResources;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Core;

/// <summary>
/// <see cref="ResourceSpace"/> core data is produced by the <see cref="ResourceSpaceDataBuilder"/>:
/// this contains the final <see cref="ResPackage"/> topologically ordered.
/// <para>
/// This can now be consumed by a <see cref="ResourceSpaceBuilder"/> that can be configured with resource handlers
/// to eventually produce a <see cref="ResourceSpace"/>.
/// </para>
/// </summary>
public sealed partial class ResourceSpaceData
{
    readonly IReadOnlyDictionary<object, ResPackage> _packageIndex;
    readonly string _ckGenPath;
    readonly string _ckWatchFolderPath;

    // _packages, _localPackages, _allPackageResources, _exposedPackages, _reachablePackageSetCache,
    // _codePackage, _appPackage and _watchRoot are set by the ResourceSpaceDataBuilder.Build method.
    internal ImmutableArray<ResPackage> _packages;
    internal ImmutableArray<ResPackage> _localPackages;
    internal ImmutableArray<IResPackageResources> _allPackageResources;
    [AllowNull]internal IReadOnlyList<ResPackage> _exposedPackages;
    [AllowNull]internal IResPackageDataCache _resPackageDataCache;
    [AllowNull]internal ResPackage _codePackage;
    [AllowNull]internal ResPackage _appPackage;
    internal string? _watchRoot;

    internal ResourceSpaceData( string ckGenPath, string cKWatchFolderPath, IReadOnlyDictionary<object, ResPackage> packageIndex )
    {
        _ckGenPath = ckGenPath;
        _ckWatchFolderPath = cKWatchFolderPath;
        _packageIndex = packageIndex;
    }

    /// <inheritdoc cref="ResourceSpaceCollector.CKGenPath"/>
    public string CKGenPath => _ckGenPath;

    /// <summary>
    /// Gets the packages indexed by their <see cref="ResPackage.FullName"/>, <see cref="ResPackage.Type"/> (if
    /// the package is defined by a type), and by the <see cref="CodeStoreResources.Code"/> and <see cref="CodeStoreResources.Store"/>
    /// resource containers of <see cref="ResPackage.Resources"/> and <see cref="ResPackage.ResourcesAfter"/>.
    /// </summary>
    public IReadOnlyDictionary<object, ResPackage> PackageIndex => _packageIndex;

    /// <summary>
    /// Gets the "&lt;Code&gt;" special head package.
    /// </summary>
    public ResPackage CodePackage => _codePackage;

    /// <summary>
    /// Gets the packages topologically ordered. <see cref="ResPackage.Index"/> is the index in this array.
    /// This list is 1-based: the 0 index is invalid.
    /// <para>
    /// This first package is <see cref="CodePackage"/> and the last one is <see cref="AppPackage"/>.
    /// </para>
    /// </summary>
    public IReadOnlyList<ResPackage> Packages => _packages;

    /// <summary>
    /// Gets the "&lt;App&gt;" special tail package.
    /// <para>
    /// When <see cref="ResourceSpaceCollector.AppResourcesLocalPath"/> is defined, its <see cref="ResPackage.LocalPath"/>
    /// is the AppResourcesLocalPath.
    /// </para>
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

    /// <summary>
    /// Gets the cache from wich <see cref="ResPackageDataHandler{T}"/> can be built.
    /// </summary>
    public IResPackageDataCache ResPackageDataCache => _resPackageDataCache;

    /// <summary>
    /// Gets the watch root. Null if no local packages exist and "&lt;App&gt;" package
    /// has no defined folder (<see cref="ResourceSpaceCollector.AppResourcesLocalPath"/> was not set).
    /// <para>
    /// This is also null if <see cref="CKWatchFolderPath"/> is <see cref="ResourceSpaceCollector.NoLiveState"/>.
    /// </para>
    /// </summary>
    public string? WatchRoot => _watchRoot;

    /// <summary>
    /// Gets the folder that contains the Live state.
    /// Can be <see cref="ResourceSpaceCollector.NoLiveState"/>.
    /// </summary>
    public string CKWatchFolderPath => _ckWatchFolderPath;
}
