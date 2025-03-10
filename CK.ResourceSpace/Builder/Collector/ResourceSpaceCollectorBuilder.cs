using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Xml;

namespace CK.Core;

/// <summary>
/// Builder for <see cref="ResourceSpaceCollector"/> that is the first step to produce a
/// <see cref="ResourceSpace"/>. This enable resource packages to be regitered from a type (that must be decorated
/// with at least one <see cref="IEmbeddedResourceTypeAttribute"/> attribute) or from any <see cref="IResourceContainer"/>.
/// </summary>
public sealed class ResourceSpaceCollectorBuilder
{
    // Packages are indexed by their FullName, their Type if package is defined
    // by type and by their IResourceContainer PackageResources.
    // The IResourceContainer is used by this builder only to check that no resource
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

    /// <summary>
    /// Produces the set of <see cref="ResPackageDescriptor"/> initialized from their <see cref="ResPackageDescriptor.Type"/>
    /// or, if there is no definer type, from the "Package.xml" file in the <see cref="ResPackageDescriptor.PackageResources"/>.
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
                var descriptor = r.PackageResources.GetResource( "Package.xml" );
                if( descriptor.IsValid )
                {
                    monitor.Warn( $"Found {descriptor} for type '{r.Type:N}'. Ignored." );
                }
            }
            else
            {
                var descriptor = r.PackageResources.GetResource( "Package.xml" );
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
                ? new ResourceSpaceCollector( _packageIndex, _packages, _localPackageCount )
                : null;
    }
}
