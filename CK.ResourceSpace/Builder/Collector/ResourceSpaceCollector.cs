using CK.EmbeddedResources;
using System;
using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Collected package descriptors for a resource space. This enables <see cref="ResPackageDescriptor"/>
/// to be mutated before <see cref="ResourceSpaceDataBuilder"/> topologically sorts them.
/// </summary>
public sealed class ResourceSpaceCollector
{
    readonly Dictionary<object, ResPackageDescriptor> _packageIndex;
    readonly List<ResPackageDescriptor> _packages;
    readonly int _localPackageCount;
    readonly int _typedPackageCount;
    IResourceContainer? _generatedCodeContainer;
    string? _appResourcesLocalPath;

    internal ResourceSpaceCollector( Dictionary<object, ResPackageDescriptor> packageIndex,
                                     List<ResPackageDescriptor> packages,
                                     int localPackageCount,
                                     IResourceContainer? generatedCodeContainer,
                                     string? appResourcesLocalPath,
                                     int typedPackageCount )
    {
        _packageIndex = packageIndex;
        _packages = packages;
        _localPackageCount = localPackageCount;
        _generatedCodeContainer = generatedCodeContainer;
        _appResourcesLocalPath = appResourcesLocalPath;
        _typedPackageCount = typedPackageCount;
    }

    /// <summary>
    /// Gets the packahe index. <see cref="FindByFullName(string)"/> or <see cref="FindByType(Type)"/>
    /// should be used to fine a package descriptor.
    /// </summary>
    public IReadOnlyDictionary<object, ResPackageDescriptor> PackageIndex => _packageIndex;

    /// <summary>
    /// Gets the package descriptors.
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
    /// Gets or sets the Code generated resource container.
    /// When let to null, an empty container is used.
    /// </summary>
    public IResourceContainer? GeneratedCodeContainer { get => _generatedCodeContainer; set => _generatedCodeContainer = value; }

    /// <summary>
    /// Gets or sets the path of the application local resources.
    /// When let to null, an empty container is used.
    /// </summary>
    public string? AppResourcesLocalPath { get => _appResourcesLocalPath; set => _appResourcesLocalPath = value; }
}
