using CK.EmbeddedResources;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CK.Core;

/// <summary>
/// Builder for <see cref="ResSpaceCollector"/> that is the first step to produce a
/// <see cref="ResSpace"/>. 
/// <para>
/// This initial builder collects all the fundamental information required to fully update a target
/// code generated folder including the Live state.
/// </para>
/// </summary>
public sealed class ResSpaceConfiguration
{
    readonly CoreCollector _coreCollector;
    IResourceContainer? _generatedCodeContainer;
    string? _appResourcesLocalPath;
    string? _liveStatePath;

    /// <summary>
    /// Initializes a new collector that must be configured.
    /// </summary>
    public ResSpaceConfiguration()
    {
        _coreCollector = new CoreCollector();
    }

    /// <summary>
    /// Gets or sets the "&lt;Code&gt;" head package generated resource container.
    /// When let to null, an empty container is considered until a non null container is configured
    /// with <see cref="ResSpaceCollector.GeneratedCodeContainer"/>, <see cref="ResSpaceDataBuilder.GeneratedCodeContainer"/>,
    /// <see cref="ResSpaceData.GeneratedCodeContainer"/> or (last chance) with <see cref="ResSpaceBuilder.GeneratedCodeContainer"/>.
    /// <para>
    /// This possibly late assignation of the code generated container enables code generator to be able to generate code
    /// based on the topologically ordered <see cref="ResSpaceData.Packages"/>. Not all code generators require this:
    /// some can assign the code from this initial configuration. 
    /// </para>
    /// </summary>
    [DisallowNull]
    public IResourceContainer? GeneratedCodeContainer
    {
        get => _generatedCodeContainer;
        set
        {
            Throw.CheckNotNullArgument( value );
            _generatedCodeContainer = value;
        }
    }

    /// <summary>
    /// Gets or sets the path of the "&lt;App&gt;" tail package application local resources 
    /// (the <see cref="ResSpaceData.AppPackage"/>'s <see cref="ResPackage.Resources"/>' <see cref="IResPackageResources.LocalPath"/>).
    /// When let to null, an empty container is used.
    /// <para>
    /// When not null, this path is fully qualified and ends with <see cref="Path.DirectorySeparatorChar"/>
    /// and will be created if it doesn't exist.
    /// </para>
    /// </summary>
    public string? AppResourcesLocalPath
    {
        get => _appResourcesLocalPath;
        set
        {
            if( value != null )
            {
                Throw.CheckArgument( Path.IsPathFullyQualified( value ) );
                value = Path.GetFullPath( value );
                if( value[^1] != Path.DirectorySeparatorChar )
                {
                    value += Path.DirectorySeparatorChar;
                }
            }
            _appResourcesLocalPath = value;
        }
    }

    /// <summary>
    /// Gets or sets the folder that contains the Live state.
    /// This is optional as this defaults to "<see cref="AppResourcesLocalPath"/>/.ck-watch/".
    /// It will be created if it doesn't exist.
    /// <para>
    /// By setting it to <see cref="ResSpaceCollector.NoLiveState"/>, no Live state is created
    /// even if <see cref="AppResourcesLocalPath"/> is specified.
    /// </para>
    /// </summary>
    public string? LiveStatePath
    {
        get => _liveStatePath;
        set
        {
            if( value != null )
            {
                if( value != ResSpaceCollector.NoLiveState )
                {
                    Throw.DebugAssert( ResSpaceCollector.NoLiveState == "none" );
                    Throw.CheckArgument( """LiveStatePath must be "none" or a fully qualified path.""", Path.IsPathFullyQualified( value ) );
                    value = Path.GetFullPath( value );
                    if( value[^1] != Path.DirectorySeparatorChar )
                    {
                        value += Path.DirectorySeparatorChar;
                    }
                }
            }
            _liveStatePath = value;
        }
    }

    /// <summary>
    /// Registers a package. It must not already exist: <paramref name="fullName"/>, <paramref name="resourceStore"/>
    /// or <paramref name="resourceAfterStore"/> must not have been already registered.
    /// <para>
    /// <paramref name="resourceStore"/> and <paramref name="resourceAfterStore"/> must be <see cref="IResourceContainer.IsValid"/>
    /// and must not be the same instance.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="fullName">The package full name.</param>
    /// <param name="defaultTargetPath">The default target path associated to the package's resources.</param>
    /// <param name="resourceStore">
    /// The package resources. Must be <see cref="IResourceContainer.IsValid"/>.
    /// </param>
    /// <param name="resourceAfterStore">
    /// The package resources to apply after the <see cref="ResPackageDescriptor.Children"/>.
    /// Must be <see cref="IResourceContainer.IsValid"/>.
    /// </param>
    /// <returns>The package descriptor on success, null on error.</returns>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  string fullName,
                                                  NormalizedPath defaultTargetPath,
                                                  IResourceContainer resourceStore,
                                                  IResourceContainer resourceAfterStore )
    {
        return _coreCollector.RegisterPackage( monitor, fullName, defaultTargetPath, resourceStore, resourceAfterStore );
    }

    /// <summary>
    /// Registers a package. It must not already exist: <paramref name="type"/> must not have been already registered.
    /// <para>
    /// The <see cref="ResPackageDescriptor.DefaultTargetPath"/> is derived from the type's namespace:
    /// "The/Type/Namespace" (the dots of the namespace are replaced with a '/'.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The type that defines the package.</param>
    /// <param name="ignoreLocal">
    /// True to ignore local folder: even if the resources are in a local folder, this creates
    /// a <see cref="AssemblyResourceContainer"/> instead of a <see cref="FileSystemResourceContainer"/>
    /// for both <see cref="ResPackageDescriptor.Resources"/> and <see cref="ResPackageDescriptor.AfterResources"/>.
    /// <para>
    /// This is mainly for tests.
    /// </para>
    /// </param>
    /// <returns>The package descriptor on success, null on error.</returns>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor, Type type, bool ignoreLocal = false )
    {
        var targetPath = type.Namespace?.Replace( '.', '/' ) ?? string.Empty;
        return _coreCollector.RegisterPackage( monitor, type, targetPath, ignoreLocal );
    }

    /// <summary>
    /// Registers a package. It must not already exist: <paramref name="type"/> must not have been already registered.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The type that defines the package.</param>
    /// <param name="defaultTargetPath">The default target path associated to the package's resources.</param>
    /// <param name="ignoreLocal">
    /// True to ignore local folder: even if the resources are in a local folder, this creates
    /// a <see cref="AssemblyResourceContainer"/> instead of a <see cref="FileSystemResourceContainer"/>
    /// for both <see cref="ResPackageDescriptor.Resources"/> and <see cref="ResPackageDescriptor.AfterResources"/>.
    /// <para>
    /// This is mainly for tests.
    /// </para>
    /// </param>
    /// <returns>The package descriptor on success, null on error.</returns>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  Type type,
                                                  NormalizedPath defaultTargetPath,
                                                  bool ignoreLocal = false )
    {
        return _coreCollector.RegisterPackage( monitor, type, defaultTargetPath, ignoreLocal );
    }

    /// <summary>
    /// Produces the set of <see cref="ResPackageDescriptor"/> initialized from their <see cref="ResPackageDescriptor.Type"/>
    /// or, if there is no definer type, from the "Package.xml" file in the <see cref="ResPackageDescriptor.Resources"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>The collector with initialized packages or null on error.</returns>
    public ResSpaceCollector? Build( IActivityMonitor monitor )
    {
        var liveStatePath = _liveStatePath
                                        ?? (_appResourcesLocalPath == null
                                                ? ResSpaceCollector.NoLiveState
                                                : _appResourcesLocalPath + ".ck-watch" + Path.DirectorySeparatorChar);
        return new ResSpaceCollector( _coreCollector,
                                           _generatedCodeContainer,
                                           _appResourcesLocalPath,
                                           liveStatePath );
    }

}
