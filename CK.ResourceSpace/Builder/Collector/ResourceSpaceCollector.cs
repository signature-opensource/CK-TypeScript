using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CK.Core;

/// <summary>
/// Exposes mutable <see cref="ResPackageDescriptor"/> for a resource space.
/// This enable resource packages to be registered from a type (that must be decorated
/// with at least one <see cref="IEmbeddedResourceTypeAttribute"/> attribute) or from any <see cref="IResourceContainer"/>
/// in any order.
/// <para>
/// This is the input of the <see cref="ResourceSpaceDataBuilder"/> that topologically sorts
/// the descriptors to produce the immutable <see cref="ResourceSpaceData.Packages"/>.
/// </para>
/// </summary>
public sealed class ResourceSpaceCollector
{
    /// <summary>
    /// Marker for <see cref="LiveStatePath"/> when no Live state must be created.
    /// </summary>
    public const string NoLiveState = "none";

    readonly CoreCollector _coreCollector;
    readonly string _ckGenPath;
    readonly string? _appResourcesLocalPath;
    readonly string _liveStatePath;
    IResourceContainer? _generatedCodeContainer;

    internal ResourceSpaceCollector( CoreCollector coreCollector,
                                     IResourceContainer? generatedCodeContainer,
                                     string ckGenPath,
                                     string? appResourcesLocalPath,
                                     string liveStatePath )
    {
        _coreCollector = coreCollector;
        _generatedCodeContainer = generatedCodeContainer;
        _ckGenPath = ckGenPath;
        _appResourcesLocalPath = appResourcesLocalPath;
        _liveStatePath = liveStatePath;
    }

    internal bool CloseRegistrations( IActivityMonitor monitor ) => _coreCollector.Close( monitor );

    /// <summary>
    /// Gets the package index. <see cref="FindByFullName(string)"/> or <see cref="FindByType(Type)"/>
    /// should be used to find a package descriptor.
    /// </summary>
    public IReadOnlyDictionary<object, ResPackageDescriptor> PackageIndex => _coreCollector.PackageIndex;

    /// <summary>
    /// Gets the package descriptors.
    /// <para>
    /// This doesn't contain the "&lt;Code&gt;" and "&lt;App&gt;" packages: the <see cref="ResourceSpaceDataBuilder"/>
    /// generates them in the <see cref="ResourceSpaceData"/>.
    /// </para>
    /// </summary>
    public IReadOnlyCollection<ResPackageDescriptor> Packages => _coreCollector.Packages;

    /// <summary>
    /// Gets the number of local packages in <see cref="Packages"/> (excluding <see cref="AppResourcesLocalPath"/>).
    /// </summary>
    public int LocalPackageCount => _coreCollector.LocalPackageCount;

    /// <summary>
    /// Gets the number of packages that are defined by a <see cref="ResPackageDescriptor.Type"/>.
    /// </summary>
    public int TypedPackageCount => _coreCollector.TypedPackageCount;

    /// <summary>
    /// Finds a mutable package descriptor by its full name.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    /// <returns>The package or null if not found.</returns>
    public ResPackageDescriptor? FindByFullName( string fullName ) => PackageIndex.GetValueOrDefault( fullName );

    /// <summary>
    /// Finds a mutable package descriptor by its type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The package or null if not found.</returns>
    public ResPackageDescriptor? FindByType( Type type ) => PackageIndex.GetValueOrDefault( type );

    /// <summary>
    /// Gets or sets the configured Code generated resource container.
    /// This can only be set if this has not been previously set (ie. this is null).
    /// See <see cref="ResourceSpaceConfiguration.GeneratedCodeContainer"/>.
    /// </summary>
    [DisallowNull]
    public IResourceContainer? GeneratedCodeContainer
    {
        get => _generatedCodeContainer;
        set
        {
            Throw.CheckNotNullArgument( value );
            Throw.CheckState( "This can be set only once.", GeneratedCodeContainer is null );
            _generatedCodeContainer = value;
        }
    }

    /// <summary>
    /// Gets the file system code generated target path. This ends with <see cref="Path.DirectorySeparatorChar"/>.
    /// See <see cref="ResourceSpaceConfiguration.CKGenPath"/>.
    /// </summary>
    public string CKGenPath => _ckGenPath;

    /// <summary>
    /// Gets the path of the application local resources. See <see cref="ResourceSpaceConfiguration.AppResourcesLocalPath"/>.
    /// </summary>
    public string? AppResourcesLocalPath => _appResourcesLocalPath;

    /// <summary>
    /// Gets the folder that contains the Live state.
    /// <see cref="NoLiveState"/> if no Live state must be created.
    /// See <see cref="ResourceSpaceConfiguration.LiveStatePath"/>.
    /// </summary>
    public string LiveStatePath => _liveStatePath;

    /// <inheritdoc cref="ResourceSpaceConfiguration.RegisterPackage(IActivityMonitor, string, NormalizedPath, IResourceContainer, IResourceContainer)"/>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  string fullName,
                                                  NormalizedPath defaultTargetPath,
                                                  IResourceContainer resourceStore,
                                                  IResourceContainer resourceAfterStore )
    {
        return _coreCollector.RegisterPackage( monitor, fullName, defaultTargetPath, resourceStore, resourceAfterStore );
    }

    /// <inheritdoc cref="ResourceSpaceConfiguration.RegisterPackage(IActivityMonitor, Type, NormalizedPath)"/>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  Type type,
                                                  NormalizedPath defaultTargetPath )
    {
        return _coreCollector.RegisterPackage( monitor, type, defaultTargetPath );
    }
}
