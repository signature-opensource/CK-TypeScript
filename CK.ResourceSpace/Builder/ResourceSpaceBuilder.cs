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

public sealed class ResourceSpaceBuilder
{
    readonly List<ResourceSpaceFolderHandler> _folderHandlers;
    readonly List<ResourceSpaceFileHandler> _fileHandlers;
    // Packages are indexed by their FullName, Type if package is defined
    // by type and by their IResourceContainer PackageResources.
    // The IResourceContainer is used by the builder only to check that no resource
    // containers are shared by 2 packages.
    // The ResourceSpace uses this index to efficiently get the definer package
    // of any ResourceLocator.
    readonly Dictionary<object, ResPackageDescriptor> _packageIndex;
    // Live tracking: LocalPackages only.
    readonly List<ResPackageDescriptor> _localPackages;
    int _packageCount;

    public ResourceSpaceBuilder()
    {
        _packageIndex = new Dictionary<object, ResPackageDescriptor>();
        _localPackages = new List<ResPackageDescriptor>();
        _folderHandlers = new List<ResourceSpaceFolderHandler>();
        _fileHandlers = new List<ResourceSpaceFileHandler>();
    }

    public IReadOnlyList<ResourceSpaceFolderHandler> FolderHandlers => _folderHandlers;

    public IReadOnlyList<ResourceSpaceFileHandler> FileHandlers => _fileHandlers;

    /// <summary>
    /// Adds a <see cref="ResourceSpaceFileHandler"/>.
    /// <para>
    /// If a handler with a common <see cref="ResourceSpaceHandler.FileExtensions"/> already
    /// exists, this is an error.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="h">The handler to register.</param>
    /// <returns>True on success, false on error.</returns>
    public bool RegisterHandler( IActivityMonitor monitor, ResourceSpaceFileHandler h )
    {
        Throw.CheckNotNullArgument( h );
        if( _fileHandlers.Contains( h ) )
        {
            monitor.Warn( $"Duplicate handler registration for '{h}'. Ignored." );
            return true;
        }
        var conflict = _fileHandlers.Where(
                            existing => existing.FileExtensions.Any(
                                            f => h.FileExtensions.Any( e => e.Equals( f, StringComparison.OrdinalIgnoreCase ) ) ) );
        if( conflict.Any() )
        {
            monitor.Error( $"Unable to add handler '{h}'. At least one file extension is already handled by '{conflict.First()}'." );
            return false;
        }
        _fileHandlers.Add( h );
        return true;
    }

    /// <summary>
    /// Adds a <see cref="ResourceSpaceFolderHandler"/>. 
    /// <para>
    /// If a handler with the same <see cref="ResourceSpaceFolderHandler.RootFolderName"/> already
    /// exists, this is an error.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="h">The handler to register.</param>
    /// <returns>True on success, false on error.</returns>
    public bool RegisterHandler( IActivityMonitor monitor, ResourceSpaceFolderHandler h )
    {
        Throw.CheckNotNullArgument( h );
        if( _folderHandlers.Contains( h ) )
        {
            monitor.Warn( $"Duplicate handler registration for '{h}'. Ignored." );
            return true;
        }
        var conflict = _folderHandlers.Where( existing => h.RootFolderName.Equals( existing.RootFolderName, StringComparison.OrdinalIgnoreCase ) );
        if( conflict.Any() )
        {
            monitor.Error( $"Unable to add handler '{h}'. This folder is already handled by {conflict.First()}." );
            return false;
        }
        _folderHandlers.Add( h );
        return true;
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
        var p = new ResPackageDescriptor( fullName, type, defaultTargetPath, packageResources, localPath );
        ++_packageCount;
        _packageIndex.Add( fullName, p );
        _packageIndex.Add( packageResources, p );
        if( type != null )
        {
            _packageIndex.Add( type, p );
        }
        if( localPath != null )
        {
            _localPackages.Add( p );
        }
        return p;
    }

    public ResourceSpace? Build( IActivityMonitor monitor )
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
        var b = ImmutableArray.CreateBuilder<ResPackage>( _packageCount );
        Throw.DebugAssert( sortResult.SortedItems != null );
        Throw.DebugAssert( "No items, only containers (and maybe groups).",
                            sortResult.SortedItems.All( s => s.IsGroup || s.IsGroupHead ) );

        var packageIndex = new Dictionary<object, ResPackage>( _packageIndex.Count + _packageCount );
        var space = new ResourceSpace( packageIndex, _folderHandlers.ToImmutableArray(), _fileHandlers.ToImmutableArray() );
        foreach( var s in sortResult.SortedItems )
        {
            if( s.IsGroup )
            {
                // Close the CodeGen resources.
                s.Item.CodeGenResources.Close();
                // Requirements and chidren have already been indexed.
                var p = new ResPackage( space,
                                        s.Item,
                                        s.Requires.Select( s => packageIndex[s.Item.CodeGenResources] ).ToImmutableArray(),
                                        s.Children.Select( s => packageIndex[s.Item.CodeGenResources] ).ToImmutableArray(),
                                        b.Count );
                // The 4 indexes.
                packageIndex.Add( p.FullName, p );
                packageIndex.Add( p.PackageResources, p );
                packageIndex.Add( p.CodeGenResources, p );
                if( p.Type != null )
                {
                    packageIndex.Add( p.Type, p );
                }
            }
        }
        Throw.DebugAssert( packageIndex.Count == _packageIndex.Count + _packageCount );
        space._packages = b.DrainToImmutable();
        return space.Initialize( monitor )
                ? space
                : null;
    }
}
