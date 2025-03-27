using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Xml;

namespace CK.Core;

/// <summary>
/// Builder for <see cref="ResourceSpaceCollector"/> that is the first step to produce a
/// <see cref="ResourceSpace"/>. This enable resource packages to be registered from a type (that must be decorated
/// with at least one <see cref="IEmbeddedResourceTypeAttribute"/> attribute) or from any <see cref="IResourceContainer"/>.
/// </summary>
public sealed class ResourceSpaceCollectorBuilder
{
    // Packages are indexed by their FullName, their Type if package is defined
    // by type and by their Resources Store container.
    // The IResourceContainer key is used by this builder only to check that no resource
    // containers are shared by 2 packages.
    readonly Dictionary<object, ResPackageDescriptor> _packageIndex;
    readonly List<ResPackageDescriptor> _packages;
    int _localPackageCount;
    int _typedPackageCount;
    IResourceContainer? _generatedCodeContainer;
    string? _appResourcesLocalPath;

    public ResourceSpaceCollectorBuilder()
    {
        _packageIndex = new Dictionary<object, ResPackageDescriptor>();
        _packages = new List<ResPackageDescriptor>();
    }

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
    /// The package resources. Must be <see cref="IResourceContainer.IsValid"/> and not a <see cref="CodeGenResourceContainer"/>.
    /// </param>
    /// <param name="resourceAfterStore">
    /// The package resources to apply after the <see cref="ResPackageDescriptor.Children"/>.
    /// Must be <see cref="IResourceContainer.IsValid"/> and not a <see cref="CodeGenResourceContainer"/>.
    /// </param>
    /// <returns>True on success, false otherwise.</returns>
    public bool RegisterPackage( IActivityMonitor monitor,
                                 string fullName,
                                 NormalizedPath defaultTargetPath,
                                 IResourceContainer resourceStore,
                                 IResourceContainer resourceAfterStore )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( fullName );
        Throw.CheckArgument( resourceStore is not null && resourceStore.IsValid );
        Throw.CheckArgument( resourceAfterStore is not null && resourceStore.IsValid );
        Throw.CheckArgument( resourceStore != resourceAfterStore );
        return DoRegister( monitor, fullName, null, defaultTargetPath, resourceStore, resourceAfterStore ) != null;
    }

    /// <summary>
    /// Registers a package. It must not already exist: <paramref name="type"/> must not have been already registered.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The type that defines the package.</param>
    /// <param name="defaultTargetPath">The default target path associated to the package's resources.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool RegisterPackage( IActivityMonitor monitor,
                                 Type type,
                                 NormalizedPath defaultTargetPath )
    {
        Throw.CheckNotNullArgument( type );
        Throw.CheckArgument( "Dynamic assembly is not supported.", type.FullName != null );
        if( _packageIndex.TryGetValue( type, out var already ) )
        {
            monitor.Error( $"Duplicate package registration: type '{type:C}' is already registered as '{already}'." );
            return false;
        }
        IResourceContainer resourceStore = type.CreateResourcesContainer( monitor );
        IResourceContainer resourceAfterStore = type.CreateResourcesContainer( monitor, resAfter: true );

        return resourceStore.IsValid && resourceAfterStore.IsValid
               && DoRegister( monitor, type.FullName, type, defaultTargetPath, resourceStore, resourceAfterStore ) != null;
    }

    ResPackageDescriptor? DoRegister( IActivityMonitor monitor,
                                      string fullName,
                                      Type? type,
                                      NormalizedPath defaultTargetPath,
                                      IResourceContainer resourceStore,
                                      IResourceContainer resourceAfterStore )
    {
        if( _packageIndex.TryGetValue( fullName, out var already ) )
        {
            monitor.Error( $"Duplicate package registration: FullName is already registered as '{already}'." );
            return null;
        }
        if( _packageIndex.TryGetValue( resourceStore, out already ) )
        {
            monitor.Error( $"Package resources mismatch: {resourceStore} cannot be associated to {ResPackage.ToString( fullName, type )} as it is already associated to '{already}'." );
            return null;
        }
        if( _packageIndex.TryGetValue( resourceAfterStore, out already ) )
        {
            monitor.Error( $"Package resources mismatch: {resourceAfterStore} cannot be associated to {ResPackage.ToString( fullName, type )} as it is already associated to '{already}'." );
            return null;
        }
        // The resource store may not be local (it may be an empty one) but if the resourceAfterStore
        // is local, then this package is a local one.
        // For simplicity we only keep a single local path at the package level and by design we
        // privilegiate the "/Res" over the "/Res[After]".
        // Live engine will be in charge to handle the one or more FileSystemResourceContainer at their level.
        string? localPath = resourceStore is FileSystemResourceContainer fs && fs.HasLocalFilePathSupport
                                ? fs.ResourcePrefix
                                : resourceAfterStore is FileSystemResourceContainer fsA && fsA.HasLocalFilePathSupport
                                    ? fsA.ResourcePrefix
                                    : null;
        if( localPath != null )
        {
            ++_localPackageCount;
        }
        var p = new ResPackageDescriptor( _packageIndex,
                                          fullName,
                                          type,
                                          defaultTargetPath,
                                          resources: new CodeStoreResources( resourceStore ),
                                          afterResources: new CodeStoreResources( resourceAfterStore ),
                                          localPath );
        _packages.Add( p );
        _packageIndex.Add( fullName, p );
        _packageIndex.Add( resourceStore, p );
        _packageIndex.Add( resourceAfterStore, p );
        if( type != null )
        {
            ++_typedPackageCount; 
            _packageIndex.Add( type, p );
        }
        return p;
    }

    /// <summary>
    /// Produces the set of <see cref="ResPackageDescriptor"/> initialized from their <see cref="ResPackageDescriptor.Type"/>
    /// or, if there is no definer type, from the "Package.xml" file in the <see cref="ResPackageDescriptor.Resources"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>The collector with initialized packages or null on error.</returns>
    public ResourceSpaceCollector? Build( IActivityMonitor monitor )
    {
        bool success = true;
        foreach( var r in _packages )
        {
            Throw.DebugAssert( r.Package == null );
            if( r.Type != null )
            {
                success &= r.InitializeFromType( monitor );
                // Detect a useless Package.xml for the type: currently, there's
                // no "merge" possible, tye type drives.
                // Lookup in both Code/Store.
                var descriptor = r.Resources.Store.GetResource( "Package.xml" );
                if( !descriptor.IsValid ) descriptor = r.Resources.Code.GetResource( "Package.xml" );
                if( descriptor.IsValid )
                {
                    monitor.Warn( $"Found {descriptor} for type '{r.Type:N}'. Ignored." );
                }
            }
            else
            {
                var descriptor = r.Resources.GetSingleResource( monitor, "Package.xml" );
                if( descriptor.IsValid )
                {
                    try
                    {
                        using( var s = descriptor.GetStream() )
                        using( var xmlReader = XmlReader.Create( s ) )
                        {
                            r.InitializeFromPackageDescriptor( monitor, xmlReader );
                        }
                    }
                    catch( Exception ex )
                    {
                        monitor.Error( $"While reading {descriptor}.", ex );
                    }
                }
            }
        }
        return success
                ? new ResourceSpaceCollector( _packageIndex,
                                              _packages,
                                              _localPackageCount,
                                              _generatedCodeContainer,
                                              _appResourcesLocalPath,
                                              _typedPackageCount )
                : null;
    }
}
