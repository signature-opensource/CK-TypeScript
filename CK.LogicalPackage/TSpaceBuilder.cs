using CK.Core;
using CK.EmbeddedResources;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CK.Core;

public sealed class TSpaceBuilder
{
    readonly List<ITPackageResourceHandler> _resourceHandlers;
    // Packages are indexed by their FullName and Type if package is defined
    // by type.
    readonly Dictionary<object, TPackageDescriptor> _packageIndex;
    // Live tracking: LocalPackages only.
    readonly List<TPackageDescriptor> _localPackages;
    int _packageCount;

    public TSpaceBuilder()
    {
        _packageIndex = new Dictionary<object, TPackageDescriptor>();
        _localPackages = new List<TPackageDescriptor>();
        _resourceHandlers = new List<ITPackageResourceHandler>();
    }

    public IReadOnlyList<ITPackageResourceHandler> ResourceHandlers => _resourceHandlers;

    public void AddHandler( ITPackageResourceHandler h )
    {
        Throw.CheckNotNullArgument( h );
        Throw.CheckState( !_resourceHandlers.Contains( h ) );
        _resourceHandlers.Add( h );
    }

    public TPackageDescriptor? FindByFullName( string fullName ) => _packageIndex.GetValueOrDefault( fullName );

    public TPackageDescriptor? FindByType( Type type ) => _packageIndex.GetValueOrDefault( type );

    public TPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                 string fullName,
                                                 NormalizedPath defaultTargetPath,
                                                 IResourceContainer packageResources )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( fullName );
        Throw.CheckNotNullArgument( packageResources );
        return DoRegister( monitor, fullName, null, defaultTargetPath, packageResources );
    }

    public TPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
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

    TPackageDescriptor? DoRegister( IActivityMonitor monitor,
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
        string? localPath = packageResources is FileSystemResourceContainer fs && fs.HasLocalFilePathSupport
                                ? fs.ResourcePrefix
                                : null;
        var p = new TPackageDescriptor( fullName, type, defaultTargetPath, packageResources, localPath );
        ++_packageCount;
        _packageIndex.Add( fullName, p );
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

    public TSpace? Build( IActivityMonitor monitor )
    {
        var sortResult = DependencySorter<TPackageDescriptor>.OrderItems( monitor, _packageIndex.Values, discoverers: null );
        if( !sortResult.IsComplete )
        {
            sortResult.LogError( monitor );
            return null;
        }
        var b = ImmutableArray.CreateBuilder<TPackage>( _packageCount );
        Throw.DebugAssert( sortResult.SortedItems != null );
        Throw.DebugAssert( "No items, only containers (and maybe groups).",
                            sortResult.SortedItems.All( s => s.IsGroup || s.IsGroupHead ) );
        foreach( var s in sortResult.SortedItems )
        {
            if( s.IsGroup )
            {

            }
        }
        return null;
    }
}

public sealed class TPackage
{
    internal TPackage( TPackageDescriptor d )
    {
        
    }
}

public sealed class TSpace
{

}
