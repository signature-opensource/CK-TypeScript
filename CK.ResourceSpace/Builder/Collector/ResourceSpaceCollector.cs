using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.IO;

namespace CK.Core;

/// <summary>
/// Exposes mutable <see cref="ResPackageDescriptor"/> for a resource space.
/// <para>
/// This is the input of the <see cref="ResourceSpaceDataBuilder"/> that topologically sorts
/// the descriptors to produce the immutable <see cref="ResourceSpaceData.Packages"/>.
/// </para>
/// </summary>
public sealed class ResourceSpaceCollector
{
    /// <summary>
    /// Marker for <see cref="CKWatchFolderPath"/> when no Live state must be created.
    /// </summary>
    public const string NoLiveState = "none";

    readonly Dictionary<object, ResPackageDescriptor> _packageIndex;
    readonly List<ResPackageDescriptor> _packages;
    readonly int _localPackageCount;
    readonly int _typedPackageCount;
    readonly IResourceContainer? _generatedCodeContainer;
    readonly string _ckGenPath;
    readonly string? _appResourcesLocalPath;
    readonly string _ckWatchFolderPath;

    internal ResourceSpaceCollector( Dictionary<object, ResPackageDescriptor> packageIndex,
                                     List<ResPackageDescriptor> packages,
                                     int localPackageCount,
                                     IResourceContainer? generatedCodeContainer,
                                     string ckGenPath,
                                     string? appResourcesLocalPath,
                                     string ckWatchFolderPath,
                                     int typedPackageCount )
    {
        _packageIndex = packageIndex;
        _packages = packages;
        _localPackageCount = localPackageCount;
        _generatedCodeContainer = generatedCodeContainer;
        _ckGenPath = ckGenPath;
        _appResourcesLocalPath = appResourcesLocalPath;
        _ckWatchFolderPath = ckWatchFolderPath;
        _typedPackageCount = typedPackageCount;
    }

    /// <summary>
    /// Gets the package index. <see cref="FindByFullName(string)"/> or <see cref="FindByType(Type)"/>
    /// should be used to find a package descriptor.
    /// </summary>
    public IReadOnlyDictionary<object, ResPackageDescriptor> PackageIndex => _packageIndex;

    /// <summary>
    /// Gets the package descriptors.
    /// <para>
    /// This doesn't contain the "&lt;Code&gt;" and "&lt;App&gt;" packages: it's the <see cref="ResourceSpaceDataBuilder"/>
    /// that generate them in the <see cref="ResourceSpaceData"/>.
    /// </para>
    /// </summary>
    public IReadOnlyCollection<ResPackageDescriptor> Packages => _packages;

    /// <summary>
    /// Gets the number of local packages in <see cref="Packages"/> (excluding <see cref="AppResourcesLocalPath"/>).
    /// </summary>
    public int LocalPackageCount => _localPackageCount;

    /// <summary>
    /// Gets the number of packages that are defined by a <see cref="ResPackageDescriptor.Type"/>.
    /// </summary>
    public int TypedPackageCount => _typedPackageCount;

    /// <summary>
    /// Finds a mutable package descriptor by its full name.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    /// <returns>The package or null if not found.</returns>
    public ResPackageDescriptor? FindByFullName( string fullName ) => _packageIndex.GetValueOrDefault( fullName );

    /// <summary>
    /// Finds a mutable package descriptor by its type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The package or null if not found.</returns>
    public ResPackageDescriptor? FindByType( Type type ) => _packageIndex.GetValueOrDefault( type );

    /// <summary>
    /// Gets the Code generated resource container. See <see cref="ResourceSpaceCollectorBuilder.GeneratedCodeContainer"/>.
    /// </summary>
    public IResourceContainer? GeneratedCodeContainer => _generatedCodeContainer;

    /// <summary>
    /// Gets the file system code generated target path. This ends with <see cref="Path.DirectorySeparatorChar"/>.
    /// See <see cref="ResourceSpaceCollectorBuilder.CKGenPath"/>.
    /// </summary>
    public string CKGenPath => _ckGenPath;

    /// <summary>
    /// Gets the path of the application local resources. See <see cref="ResourceSpaceCollectorBuilder.AppResourcesLocalPath"/>.
    /// </summary>
    public string? AppResourcesLocalPath => _appResourcesLocalPath;

    /// <summary>
    /// Gets the folder that contains the Live state.
    /// <see cref="NoLiveState"/> if no Live state must be created.
    /// See <see cref="ResourceSpaceCollectorBuilder.CKWatchFolderPath"/>.
    /// </summary>
    public string CKWatchFolderPath => _ckWatchFolderPath;
}
