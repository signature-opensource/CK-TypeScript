using CK.EmbeddedResources;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core;

/// <summary>
/// <see cref="ResSpace"/> core data is produced by the <see cref="ResSpaceDataBuilder"/>:
/// this contains the final <see cref="ResPackage"/> topologically ordered.
/// <para>
/// This can now be consumed by a <see cref="ResSpaceBuilder"/> that can be configured with resource handlers
/// to eventually produce a <see cref="ResSpace"/>.
/// </para>
/// </summary>
public sealed partial class ResSpaceData
{
    readonly IReadOnlyDictionary<object, ResPackage> _packageIndex;
    readonly IReadOnlyDictionary<IResourceContainer, IResPackageResources> _resourceIndex;
    readonly string _liveStatePath;
    // Last mutable code container. Duplicating it is required, because even if we inspect
    // the ResourceContainerWrapper.InnerContainer, an empty container may be a non yet
    // assigned container or a definitly assigned empty one.
    IResourceContainer? _generatedCodeContainer;

    // _packages, _localPackages, _allPackageResources, _exposedPackages, _reachablePackageSetCache,
    // _codePackage, _appPackage and _watchRoot are set by the ResourceSpaceDataBuilder.Build method.
    internal ImmutableArray<ResPackage> _packages;
    internal ImmutableArray<ResPackage> _localPackages;
    internal ImmutableArray<IResPackageResources> _allPackageResources;
    [AllowNull]internal ISpaceDataCache _resPackageDataCache;
    [AllowNull]internal ResPackage _codePackage;
    [AllowNull]internal ResPackage _appPackage;
    internal string? _watchRoot;

    internal ResSpaceData( IResourceContainer? generatedCodeContainer,
                           string cKWatchFolderPath,
                           IReadOnlyDictionary<object, ResPackage> packageIndex,
                           IReadOnlyDictionary<IResourceContainer, IResPackageResources> resourceIndex )
    {
        _generatedCodeContainer = generatedCodeContainer;
        _liveStatePath = cKWatchFolderPath;
        _packageIndex = packageIndex;
        _resourceIndex = resourceIndex;
    }

    /// <summary>
    /// Gets or sets the configured Code generated resource container.
    /// This can only be set if this has not been previously set (ie. this is null).
    /// See <see cref="ResSpaceConfiguration.GeneratedCodeContainer"/>.
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
            // Ignore auto assignation: this just lock the container
            // (If the value is another ResourceContainerWrapper, this will throw and it's fine: we
            // don't want cycles!)
            if( _codePackage.AfterResources.Resources != value )
            {
                ((ResourceContainerWrapper)_codePackage.AfterResources.Resources).InnerContainer = value;
            }
        }
    }

    /// <summary>
    /// Gets the packages indexed by their <see cref="ResPackage.FullName"/> and <see cref="ResPackage.Type"/> (if
    /// the package is defined by a type).
    /// </summary>
    public IReadOnlyDictionary<object, ResPackage> PackageIndex => _packageIndex;

    /// <summary>
    /// Gets the "&lt;Code&gt;" head package.
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
    /// Gets the local packages.
    /// </summary>
    public ImmutableArray<ResPackage> LocalPackages => _localPackages;

    /// <summary>
    /// Gets the "&lt;App&gt;" tail package.
    /// <para>
    /// When <see cref="ResSpaceCollector.AppResourcesLocalPath"/> is defined, its <see cref="ResPackage.LocalPath"/>
    /// is the AppResourcesLocalPath.
    /// </para>
    /// </summary>
    public ResPackage AppPackage => _appPackage;

    /// <summary>
    /// Gets all the topologically ordered <see cref="IResPackageResources"/> indexed
    /// by their <see cref="IResPackageResources.Index"/>.
    /// </summary>
    public ImmutableArray<IResPackageResources> AllPackageResources => _allPackageResources;

    /// <summary>
    /// Gets the <see cref="IResPackageResources"/> from a <see cref="IResourceContainer"/>.
    /// </summary>
    /// <param name="resourceContainer">The resource container.</param>
    /// <param name="packageResources">The corresponding package resources.</param>
    /// <returns>True when found, false otherwise.</returns>
    public bool TryGetPackageResources( IResourceContainer resourceContainer, [NotNullWhen( true )] out IResPackageResources? packageResources )
    {
        return _resourceIndex.TryGetValue( resourceContainer, out packageResources );
    }

    /// <summary>
    /// Gets the <see cref="IResPackageResources"/> from a <see cref="IResourceContainer"/> or throws an <see cref="System.ArgumentException"/>.
    /// </summary>
    /// <param name="resourceContainer">The resource container.</param>
    /// <returns>The corresponding package resources.</returns>
    public IResPackageResources GetPackageResources( IResourceContainer resourceContainer )
    {
        if( !_resourceIndex.TryGetValue( resourceContainer, out var packageResources ) )
        {
            Throw.ArgumentException( $"Resource conatiner {resourceContainer} not found among {_packages.Length} packages." );
        }
        return packageResources;
    }

    /// <summary>
    /// Gets the cache from wich <see cref="ResPackageDataCache{T}"/> can be built.
    /// </summary>
    public ISpaceDataCache SpaceDataCache => _resPackageDataCache;

    /// <summary>
    /// Gets the folder that contains the Live state.
    /// Can be <see cref="ResSpaceCollector.NoLiveState"/>.
    /// </summary>
    public string LiveStatePath => _liveStatePath;

    /// <summary>
    /// Gets the watch root. Null if no local packages exist and "&lt;App&gt;" package
    /// has no defined folder (<see cref="ResSpaceCollector.AppResourcesLocalPath"/> was not set).
    /// <para>
    /// This is also null if <see cref="LiveStatePath"/> is <see cref="ResSpaceCollector.NoLiveState"/>.
    /// </para>
    /// </summary>
    public string? WatchRoot => _watchRoot;

}
