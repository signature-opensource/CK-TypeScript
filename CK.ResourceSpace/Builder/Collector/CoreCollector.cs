using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

sealed partial class CoreCollector : IResPackageDescriptorRegistrar
{
    // Packages are indexed by their FullName, their Type if package is defined
    // by type and by their Resources Store container.
    // The IResourceContainer key is used by this builder only to check that no resource
    // containers are shared by 2 packages.
    readonly Dictionary<object, ResPackageDescriptor> _packageIndex;
    readonly List<ResPackageDescriptor> _packages;
    // Contains the package initially optional. After the topological
    // sort, they may be no more optional.
    readonly List<ResPackageDescriptor> _optionalPackages;
    readonly ResPackageDescriptorContext _packageDescriptorContext;
    // Set by ResSpaceConfiguration.
    internal IResPackageDescriptorResolver? _packageResolver;
    internal bool _revertOrderingNames;


    int _localPackageCount;
    int _typedPackageCount;

    public CoreCollector()
    {
        _packageIndex = new Dictionary<object, ResPackageDescriptor>();
        _packages = new List<ResPackageDescriptor>();
        _optionalPackages = new List<ResPackageDescriptor>();
        _packageDescriptorContext = new ResPackageDescriptorContext( _packageIndex );
    }

    public ResPackageDescriptor? FindByFullName( string fullName ) => _packageIndex.GetValueOrDefault( fullName );

    public ResPackageDescriptor? FindByType( Type type ) => _packageIndex.GetValueOrDefault( type );

    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  string fullName,
                                                  NormalizedPath defaultTargetPath,
                                                  IResourceContainer resourceStore,
                                                  IResourceContainer resourceAfterStore,
                                                  bool? isOptional )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( fullName );
        Throw.CheckArgument( resourceStore is not null && resourceStore.IsValid );
        Throw.CheckArgument( resourceAfterStore is not null && resourceStore.IsValid );
        Throw.CheckArgument( resourceStore != resourceAfterStore );
        return DoRegister( monitor, fullName, null, defaultTargetPath, resourceStore, resourceAfterStore, isOptional );
    }

    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor, Type type, bool? isOptional, bool ignoreLocal = false )
    {
        var targetPath = type.Namespace?.Replace( '.', '/' ) ?? string.Empty;
        return RegisterPackage( monitor, type, targetPath, isOptional, ignoreLocal );
    }

    public ResPackageDescriptor? RegisterPackage( IActivityMonitor monitor,
                                                  Type type,
                                                  NormalizedPath defaultTargetPath,
                                                  bool? isOptional,
                                                  bool ignoreLocal )
    {
        Throw.CheckNotNullArgument( type );
        Throw.CheckArgument( "Dynamic assembly is not supported.", type.FullName != null );
        IResourceContainer resourceStore = type.CreateResourcesContainer( monitor, ignoreLocal: ignoreLocal );
        IResourceContainer resourceAfterStore = type.CreateResourcesContainer( monitor, resAfter: true, ignoreLocal: ignoreLocal );

        return resourceStore.IsValid && resourceAfterStore.IsValid
               ? DoRegister( monitor, type.FullName, type, defaultTargetPath, resourceStore, resourceAfterStore, isOptional )
               : null;
    }

    ResPackageDescriptor? DoRegister( IActivityMonitor monitor,
                                      string fullName,
                                      Type? type,
                                      NormalizedPath defaultTargetPath,
                                      IResourceContainer resourceStore,
                                      IResourceContainer resourceAfterStore,
                                      bool? isOptional )
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
        if( type != null && _packageIndex.TryGetValue( type, out already ) )
        {
            monitor.Error( $"Duplicate package registration: Type '{type:N}' is already registered as '{already}'." );
            return null;
        }

        var p = new ResPackageDescriptor( _packageDescriptorContext,
                                          fullName,
                                          type,
                                          defaultTargetPath,
                                          resources: new StoreContainer( _packageDescriptorContext, resourceStore ),
                                          afterResources: new StoreContainer( _packageDescriptorContext, resourceAfterStore ) );
        bool initialized = type != null
                            ? p.InitializeFromType( monitor, isOptional )
                            : p.InitializeFromManifest( monitor, isOptional );
        if( !initialized )
        {
            return null;
        }
        if( p.IsOptional )
        {
            _optionalPackages.Add( p );
        }
        else
        {
            _packages.Add( p );
        }
        // Whether the package is optional or not, we index it.
        // An optional package will be removed it is still optional after the
        // topological sort (RemoveDefinitelyOptional below).
        if( p.IsLocalPackage )
        {
            ++_localPackageCount;
        }
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

    void RemoveDefinitelyOptional( ResPackageDescriptor p )
    {
        Throw.DebugAssert( p.IsOptional );
        if( p.IsLocalPackage )
        {
            --_localPackageCount;
        }
        _packageIndex.Remove( p.FullName );
        _packageIndex.Remove( p.ResourcesInnerContainer );
        _packageIndex.Remove( p.AfterResourcesInnerContainer );
        if( p.Type != null )
        {
            --_typedPackageCount;
            _packageIndex.Remove( p.Type );
        }
    }

    void SetNoMoreOptionalPackage( ResPackageDescriptor p )
    {
        Throw.DebugAssert( p.IsOptional );
        Throw.DebugAssert( !_packages.Contains( p ) );
        p._isOptional = false;
        _packages.Add( p );
    }

    public IReadOnlyDictionary<object, ResPackageDescriptor> PackageIndex => _packageIndex;

    public IReadOnlyCollection<ResPackageDescriptor> Packages => _packages;

    public int LocalPackageCount => _localPackageCount;

    public int TypedPackageCount => _typedPackageCount;

    public int SingleMappingCount => _packageDescriptorContext.SingleMappingCount;

    public bool Close( IActivityMonitor monitor, out HashSet<ResourceLocator> codeHandledResources )
    {
        codeHandledResources = _packageDescriptorContext.Close();
        var sorter = new TopologicalSorter( monitor, this );
        return sorter.Run();
    }
}
