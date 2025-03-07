using CK.Core;
using CK.EmbeddedResources;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Builder for <see cref="ResourceSpaceData"/>.
/// </summary>
public sealed class ResourceSpaceDataBuilder
{
    // Packages are indexed by their FullName, Type if package is defined
    // by type and by their IResourceContainer PackageResources.
    // The IResourceContainer is used by the builder only to check that no resource
    // containers are shared by 2 packages.
    // The ResourceSpace uses this index to efficiently get the definer package
    // of any ResourceLocator.
    readonly Dictionary<object, ResPackageDescriptor> _packageIndex;
    int _packageCount;
    int _localPackageCount;

    public ResourceSpaceDataBuilder()
    {
        _packageIndex = new Dictionary<object, ResPackageDescriptor>();
    }

    /// <summary>
    /// Finds a package by its full name.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    /// <returns>The package or null if not found.</returns>
    public ResPackageDescriptor? FindByFullName( string fullName ) => _packageIndex.GetValueOrDefault( fullName );

    /// <summary>
    /// Finds a package by its type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The package or null if not found.</returns>
    public ResPackageDescriptor? FindByType( Type type ) => _packageIndex.GetValueOrDefault( type );

    /// <summary>
    /// Registers a package. It must not already exist: <paramref name="fullName"/> or <paramref name="packageResources"/>
    /// must not have been already registered.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="fullName">The package full name.</param>
    /// <param name="defaultTargetPath">The default target path associated to the package's resources.</param>
    /// <param name="packageResources">The package resources. Must be <see cref="IResourceContainer.IsValid"/>.</param>
    /// <returns>The package decriptor on success, false otherwise.</returns>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  string fullName,
                                                  NormalizedPath defaultTargetPath,
                                                  IResourceContainer packageResources )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( fullName );
        Throw.CheckArgument( packageResources is not null && packageResources.IsValid );
        return DoRegister( monitor, fullName, null, defaultTargetPath, packageResources );
    }

    /// <summary>
    /// Registers a package. It must not already exist: <paramref name="type"/> must not have been already registered.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="type">The type that defines the package.</param>
    /// <param name="defaultTargetPath">The default target path associated to the package's resources.</param>
    /// <returns>The package decriptor on success, false otherwise.</returns>
    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  Type type,
                                                  NormalizedPath defaultTargetPath )
    {
        Throw.CheckNotNullArgument( type );
        Throw.CheckArgument( "Dynamic assembly is not supported.", type.FullName != null );
        if( _packageIndex.TryGetValue( type, out var already ) )
        {
            monitor.Error( $"Duplicate package registration: type '{type:C}' is already registered as '{already}'." );
            return null;
        }
        return DoRegister( monitor, type.FullName, type, defaultTargetPath, type.CreateResourcesContainer( monitor ) );
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
            monitor.Error( $"Package resources mismatch: {packageResources} cannot be associated to {ResPackage.ToString(fullName,type)} as it is already associated to '{already}'." );
            return null;
        }
        string? localPath = packageResources is FileSystemResourceContainer fs && fs.HasLocalFilePathSupport
                                ? fs.ResourcePrefix
                                : null;
        if( localPath != null )
        {
            ++_localPackageCount;
        }
        var p = new ResPackageDescriptor( fullName, type, defaultTargetPath, packageResources, localPath );
        ++_packageCount;
        _packageIndex.Add( fullName, p );
        _packageIndex.Add( packageResources, p );
        if( type != null )
        {
            _packageIndex.Add( type, p );
        }
        return p;
    }

    /// <summary>
    /// Tries to build the <see cref="ResourceSpaceData"/>. 
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>The space data on success, null otherwise.</returns>
    public ResourceSpaceData? Build( IActivityMonitor monitor )
    {
        var sortResult = DependencySorter<ResPackageDescriptor>.OrderItems( monitor,
                                                                            _packageIndex.Values,
                                                                            discoverers: null );
        if( !sortResult.IsComplete )
        {
            sortResult.LogError( monitor );
            return null;
        }
        // We can compute the final size of the index: it is the same as the builder index
        // with FullName, PackageResources and Type(?) plus the CodeGenResources instance.
        Throw.DebugAssert( sortResult.SortedItems != null );
        Throw.DebugAssert( "No items, only containers (and maybe groups).",
                            sortResult.SortedItems.All( s => s.IsGroup || s.IsGroupHead ) );

        var b = ImmutableArray.CreateBuilder<ResPackage>( _packageCount );
        var bLocal = ImmutableArray.CreateBuilder<ResPackage>( _localPackageCount );
        var bRoot = ImmutableArray.CreateBuilder<ResPackage>();
        var packageIndex = new Dictionary<object, ResPackage>( _packageIndex.Count + _packageCount );
        var space = new ResourceSpaceData( packageIndex );
        foreach( var s in sortResult.SortedItems )
        {
            if( s.IsGroup )
            {
                ResPackageDescriptor d = s.Item;
                // Close the CodeGen resources.
                d.CodeGenResources.Close();
                Throw.DebugAssert( "A child cannot be required and a requirement cannot be a child.",
                                   !s.Requires.Intersect( s.Children ).Any() );
                // Requirements and children have already been indexed.
                ImmutableArray<ResPackage> requires = s.Requires.Select( s => packageIndex[s.Item.CodeGenResources] ).ToImmutableArray();
                ImmutableArray<ResPackage> children = s.Children.Select( s => packageIndex[s.Item.CodeGenResources] ).ToImmutableArray();
                var p = new ResPackage( d.FullName,
                                        d.DefaultTargetPath,
                                        d.PackageResources,
                                        d.CodeGenResources,
                                        d.LocalPath,
                                        d.IsGroup,
                                        d.Type,
                                        requires,
                                        children,
                                        b.Count );
                // The 4 indexes.
                packageIndex.Add( p.FullName, p );
                packageIndex.Add( p.PackageResources, p );
                packageIndex.Add( p.CodeGenResources, p );
                if( p.Type != null )
                {
                    packageIndex.Add( p.Type, p );
                }
                // Rank is 1-based. Rank = 1 is for the head.
                if( s.Rank == 2 )
                {
                    bRoot.Add( p );
                }
                if( p.IsLocalPackage )
                {
                    bLocal.Add( p );
                }
            }
        }
        Throw.DebugAssert( packageIndex.Count == _packageIndex.Count + _packageCount );
        space._packages = b.DrainToImmutable();
        space._rootPackages = bRoot.ToImmutableArray();
        space._localPackages = bLocal.ToImmutableArray();
        return space;
    }
}
