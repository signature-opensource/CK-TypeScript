using CK.EmbeddedResources;
using System;
using System.Collections.Generic;

namespace CK.Core;

sealed class CoreCollector
{
    // Packages are indexed by their FullName, their Type if package is defined
    // by type and by their Resources Store container.
    // The IResourceContainer key is used by this builder only to check that no resource
    // containers are shared by 2 packages.
    readonly Dictionary<object, ResPackageDescriptor> _packageIndex;
    readonly List<ResPackageDescriptor> _packages;
    readonly ResPackageDescriptorContext _packageDescriptorContext;
    int _localPackageCount;
    int _typedPackageCount;

    public CoreCollector()
    {
        _packageIndex = new Dictionary<object, ResPackageDescriptor>();
        _packages = new List<ResPackageDescriptor>();
        _packageDescriptorContext = new ResPackageDescriptorContext();

    }

    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  string fullName,
                                                  NormalizedPath defaultTargetPath,
                                                  IResourceContainer resourceStore,
                                                  IResourceContainer resourceAfterStore )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( fullName );
        Throw.CheckArgument( resourceStore is not null && resourceStore.IsValid );
        Throw.CheckArgument( resourceAfterStore is not null && resourceStore.IsValid );
        Throw.CheckArgument( resourceStore != resourceAfterStore );
        return DoRegister( monitor, fullName, null, defaultTargetPath, resourceStore, resourceAfterStore );
    }

    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  Type type,
                                                  NormalizedPath defaultTargetPath )
    {
        Throw.CheckNotNullArgument( type );
        Throw.CheckArgument( "Dynamic assembly is not supported.", type.FullName != null );
        IResourceContainer resourceStore = type.CreateResourcesContainer( monitor );
        IResourceContainer resourceAfterStore = type.CreateResourcesContainer( monitor, resAfter: true );

        return resourceStore.IsValid && resourceAfterStore.IsValid
               ? DoRegister( monitor, type.FullName, type, defaultTargetPath, resourceStore, resourceAfterStore )
               : null;
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
        bool isGroup = true;
        if( type != null )
        {
            isGroup = !typeof( IResourcePackage ).IsAssignableFrom( type ); ;
            if( _packageIndex.TryGetValue( type, out already ) )
            {
                monitor.Error( $"Duplicate package registration: Type '{type:N}' is already registered as '{already}'." );
                return null;
            }
        }
        var p = new ResPackageDescriptor( _packageDescriptorContext,
                                          fullName,
                                          type,
                                          defaultTargetPath,
                                          resources: new StoreContainer( _packageDescriptorContext, resourceStore ),
                                          afterResources: new StoreContainer( _packageDescriptorContext, resourceAfterStore ) );
        p.IsGroup = isGroup;
        bool isLocal = resourceStore is FileSystemResourceContainer fs && fs.HasLocalFilePathSupport
                       || resourceAfterStore is FileSystemResourceContainer fsA && fsA.HasLocalFilePathSupport;
        if( isLocal )
        {
            ++_localPackageCount;
        }
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

    public IReadOnlyDictionary<object, ResPackageDescriptor> PackageIndex => _packageIndex;

    public IReadOnlyCollection<ResPackageDescriptor> Packages => _packages;

    public int LocalPackageCount => _localPackageCount;

    public int TypedPackageCount => _typedPackageCount;

    public bool Close( IActivityMonitor monitor )
    {
        _packageDescriptorContext.Close();
        bool success = true;
        foreach( var r in _packages )
        {
            Throw.DebugAssert( r.Package == null );
            success &= r.Initialize( monitor, _packageIndex );
        }
        return success;
    }
}
