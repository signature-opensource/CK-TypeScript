using CK.EmbeddedResources;
using System;
using System.IO;

namespace CK.Core;

/// <summary>
/// Builder for <see cref="ResourceSpaceCollector"/> that is the first step to produce a
/// <see cref="ResourceSpace"/>. 
/// <para>
/// This initial builder collects all the fundamental information required to fully update a target
/// code generated folder including the Live state.
/// </para>
/// </summary>
public sealed class ResourceSpaceConfiguration
{
    readonly CoreCollector _coreCollector;
    IResourceContainer? _generatedCodeContainer;
    string _ckGenPath;
    string? _appResourcesLocalPath;
    string? _liveStatePath;

    /// <summary>
    /// Initializes a new collector that must be configured.
    /// </summary>
    public ResourceSpaceConfiguration()
    {
        _coreCollector = new CoreCollector();
        _ckGenPath = string.Empty;
    }

    /// <summary>
    /// Required code generated target path that will be created or updated.
    /// <para>
    /// This folder is updated on each generation. It cannot be below nor above the <see cref="AppResourcesLocalPath"/>
    /// or the <see cref="LiveStatePath"/>.
    /// </para>
    /// <para>
    /// The path must be fully qualified. It is normalized to end with <see cref="Path.DirectorySeparatorChar"/>.
    /// </para>
    /// </summary>
    public string CKGenPath
    {
        get => _ckGenPath;
        set
        {
            Throw.CheckArgument( !string.IsNullOrWhiteSpace( value ) );
            Throw.CheckArgument( Path.IsPathFullyQualified( value ) );
            value = Path.GetFullPath( value );
            if( value[^1] != Path.DirectorySeparatorChar )
            {
                value += Path.DirectorySeparatorChar;
            }
            _ckGenPath = value;
        }
    }

    /// <summary>
    /// Gets or sets the "&lt;Code&gt;" head package generated resource container.
    /// When let to null, an empty container is used.
    /// </summary>
    public IResourceContainer? GeneratedCodeContainer
    {
        get => _generatedCodeContainer;
        set => _generatedCodeContainer = value;
    }

    /// <summary>
    /// Gets or sets the path of the "&lt;App&gt;" tail package application local resources.
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
    /// By setting it to <see cref="ResourceSpaceCollector.NoLiveState"/>, no Live state is created
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
                if( value != ResourceSpaceCollector.NoLiveState )
                {
                    Throw.DebugAssert( ResourceSpaceCollector.NoLiveState == "none" );
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
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The type that defines the package.</param>
    /// <param name="defaultTargetPath">The default target path associated to the package's resources.</param>
    /// <returns>The package descriptor on success, null on error.</returns>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  Type type,
                                                  NormalizedPath defaultTargetPath )
    {
        return _coreCollector.RegisterPackage( monitor, type, defaultTargetPath );
    }

    /// <summary>
    /// Produces the set of <see cref="ResPackageDescriptor"/> initialized from their <see cref="ResPackageDescriptor.Type"/>
    /// or, if there is no definer type, from the "Package.xml" file in the <see cref="ResPackageDescriptor.Resources"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>The collector with initialized packages or null on error.</returns>
    public ResourceSpaceCollector? Build( IActivityMonitor monitor )
    {
        if( string.IsNullOrEmpty( _ckGenPath ) )
        {
            monitor.Error( "CKGenPath is required." );
            return null;
        }
        if( _appResourcesLocalPath != null
            && (_appResourcesLocalPath.StartsWith( _ckGenPath ) || _ckGenPath.StartsWith( _appResourcesLocalPath )) )
        {
            monitor.Error( $"""
                Invalid AppResourcesLocalPath: it must not be above or below CKGenPath.
                CKGenPath: {_ckGenPath}
                AppResourcesLocalPath: {_appResourcesLocalPath}
                """ );
            return null;
        }
        var liveStatePath = _liveStatePath
                                        ?? (_appResourcesLocalPath == null
                                                ? ResourceSpaceCollector.NoLiveState
                                                : _appResourcesLocalPath + ".ck-watch" + Path.DirectorySeparatorChar);
        if( liveStatePath.StartsWith( _ckGenPath ) || _ckGenPath.StartsWith( liveStatePath ) )
        {
            monitor.Error( $"""
                Invalid LiveStatePath: it must not be above or below CKGenPath.
                CKGenPath: {_ckGenPath}
                LiveStatePath: {liveStatePath}
                """ );
            return null;
        }
        return new ResourceSpaceCollector( _coreCollector,
                                           _generatedCodeContainer,
                                           _ckGenPath,
                                           _appResourcesLocalPath,
                                           liveStatePath );
    }

}
