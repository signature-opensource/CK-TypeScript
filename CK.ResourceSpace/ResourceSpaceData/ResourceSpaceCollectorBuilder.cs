using CK.EmbeddedResources;
using System;
using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Builder for <see cref="ResourceSpaceCollector"/>.
/// </summary>
public sealed class ResourceSpaceCollectorBuilder
{
    // Packages are indexed by their FullName, Type if package is defined
    // by type and by their IResourceContainer PackageResources.
    // The IResourceContainer is used by the builder only to check that no resource
    // containers are shared by 2 packages.
    // The ResourceSpace uses this index to efficiently get the definer package
    // of any ResourceLocator.
    readonly Dictionary<object, ResPackageDescriptor> _packageIndex;
    readonly List<ResPackageDescriptor> _packages;
    int _localPackageCount;

    public ResourceSpaceCollectorBuilder()
    {
        _packageIndex = new Dictionary<object, ResPackageDescriptor>();
        _packages = new List<ResPackageDescriptor>();
    }

    /// <summary>
    /// Registers a package. It must not already exist: <paramref name="fullName"/> or <paramref name="packageResources"/>
    /// must not have been already registered.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="fullName">The package full name.</param>
    /// <param name="defaultTargetPath">The default target path associated to the package's resources.</param>
    /// <param name="packageResources">The package resources. Must be <see cref="IResourceContainer.IsValid"/>.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool RegisterPackage( IActivityMonitor monitor,
                                 string fullName,
                                 NormalizedPath defaultTargetPath,
                                 IResourceContainer packageResources )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( fullName );
        Throw.CheckArgument( packageResources is not null && packageResources.IsValid );
        return DoRegister( monitor, fullName, null, defaultTargetPath, packageResources ) != null;
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
        IResourceContainer resources = type.CreateResourcesContainer( monitor );
        return resources.IsValid && DoRegister( monitor, type.FullName, type, defaultTargetPath, resources ) != null;
    }

    ResPackageDescriptor? DoRegister( IActivityMonitor monitor,
                                      string fullName,
                                      Type? type,
                                      NormalizedPath defaultTargetPath,
                                      IResourceContainer packageResources )
    {
        if( _packageIndex.TryGetValue( fullName, out var already ) )
        {
            monitor.Error( $"Duplicate package registration: FullName is already registered as '{already}'." );
            return null;
        }
        if( _packageIndex.TryGetValue( packageResources, out already ) )
        {
            monitor.Error( $"Package resources mismatch: {packageResources} cannot be associated to {ResPackage.ToString( fullName, type )} as it is already associated to '{already}'." );
            return null;
        }
        string? localPath = packageResources is FileSystemResourceContainer fs && fs.HasLocalFilePathSupport
                                ? fs.ResourcePrefix
                                : null;
        if( localPath != null )
        {
            ++_localPackageCount;
        }
        var p = new ResPackageDescriptor( _packageIndex, fullName, type, defaultTargetPath, packageResources, localPath );
        _packages.Add( p );
        _packageIndex.Add( fullName, p );
        _packageIndex.Add( packageResources, p );
        if( type != null )
        {
            _packageIndex.Add( type, p );
        }
        return p;
    }

    public ResourceSpaceCollector Build( IActivityMonitor monitor )
    {
        bool success = true;
        foreach( var r in _packages )
        {
            Throw.DebugAssert( r.Package == null );
            if( r.Type != null )
            {
                success &= r.InitializeFromType( monitor );
            }
        }
        else
        {
            // Read package.xml in CodeGenResource or IResourceContainer.

        }

    }
}
