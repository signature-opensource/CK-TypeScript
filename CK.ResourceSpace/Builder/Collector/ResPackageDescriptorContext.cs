using CK.EmbeddedResources;
using System;
using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Provided context to the <see cref="ResPackageDescriptor"/>.
/// </summary>
sealed class ResPackageDescriptorContext
{
    // Duplicate reference to the package index (held by the ResSpaceCollector)
    // to enable the ResPackageDescriptor to expose AddSingleMapping methods.
    readonly Dictionary<object, ResPackageDescriptor> _packageIndex;
    readonly HashSet<ResourceLocator> _codeHandledResources;
    int _singleMappingCount;
    bool _closed;

    public ResPackageDescriptorContext( Dictionary<object, ResPackageDescriptor> packageIndex )
    {
        _codeHandledResources = new HashSet<ResourceLocator>();
        _packageIndex = packageIndex;
    }

    public bool Closed => _closed;

    /// <summary>
    /// IReadOnlySet to force the use of RegisterCodeHandledResources.
    /// StoreContainer constructor casts this to back to HashSet&lt;ResourceLocator&gt;.
    /// </summary>
    public IReadOnlySet<ResourceLocator> CodeHandledResources => _codeHandledResources;

    public int SingleMappingCount => _singleMappingCount;

    public HashSet<ResourceLocator> Close()
    {
        Throw.DebugAssert( !Closed );
        _closed = true;
        return _codeHandledResources;
    }

    public void RegisterCodeHandledResources( ResourceLocator resource )
    {
        Throw.CheckState( !Closed );
        _codeHandledResources.Add( resource );
    }

    internal bool AddSingleMapping( IActivityMonitor monitor, string alias, ResPackageDescriptor p )
    {
        Throw.DebugAssert( !string.IsNullOrWhiteSpace( alias ) );
        if( _packageIndex.TryGetValue( alias, out var exist ) )
        {
            monitor.Error( $"Name '{alias}' cannot be mapped to '{p.FullName}' as it is already mapped to '{exist.FullName}'." );
            return false;
        }
        ++_singleMappingCount;
        _packageIndex.Add( alias, p );
        return true;
    }

    internal bool AddSingleMapping( IActivityMonitor monitor, Type alias, ResPackageDescriptor p )
    {
        if( p.Type == null )
        {
            monitor.Error( $"Type '{alias:N}' cannot be mapped to '{p.FullName}' because this package is not defined by a Type." );
            return false;
        }
        if( !alias.IsAssignableFrom( p.Type ) )
        {
            monitor.Error( $"Type '{alias:N}' cannot be mapped to '{p.FullName}' because it is not assignable from the package's Type." );
            return false;
        }
        if( _packageIndex.TryGetValue( alias, out var exist ) )
        {
            monitor.Error( $"Type '{alias:N}' cannot be mapped to '{p.FullName}' as it is already mapped to '{exist.FullName}'." );
            return false;
        }
        ++_singleMappingCount;
        _packageIndex.Add( alias, p );
        return true;
    }
}
