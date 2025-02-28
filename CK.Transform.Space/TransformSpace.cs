using CK.Core;
using CK.EmbeddedResources;
using CK.Transform.Core;
using Microsoft.VisualBasic;
using System.Collections.Generic;

namespace CK.Transform.Space;


public sealed class TransformSpace
{
    readonly Dictionary<string, TransformPackage> _packageIndex;
    readonly List<TransformPackage> _packages;

    // All sources are indexed by their LogicalName.
    internal readonly Dictionary<string, TransformableSource> _itemsByName;
    // Indexed by file path only when the origin is a FileSystemResourceContainer
    // with true HasLocalFilePathSupport.
    internal readonly Dictionary<string, TransformableSource> _localItems;
    internal readonly TransformerHost _transformerHost;

    int _applyChangesVersion;

    public TransformSpace( params IEnumerable<TransformLanguage> languages )
    {
        _packageIndex = new Dictionary<string, TransformPackage>();
        _packages = new List<TransformPackage>();
        _itemsByName = new Dictionary<string, TransformableSource>();
        _localItems = new Dictionary<string, TransformableSource>();
        _transformerHost = new TransformerHost( languages );
    }

    public TransformPackage? RegisterPackage( IActivityMonitor monitor, string name )
    {
        if( _packageIndex.ContainsKey( name ) )
        {
            monitor.Error( $"Duplicate Transform package. Package '{name}' already exists." );
            return null;
        }
        var p = new TransformPackage( this, name, _packages.Count );
        _packages.Add( p );
        _packageIndex.Add( name, p );
        return p;
    }

    public void OnChange( IActivityMonitor monitor, string localFilePath )
    {
        if( _localItems.TryGetValue( localFilePath, out var source )
            && source.State != TransformableSourceState.Dirty )
        {
            source.Package.OnChange( monitor, source );
        }
    }

    public void ApplyChanges( IActivityMonitor monitor )
    {
        var c = new ApplyChangesContext( monitor, ++_applyChangesVersion, _transformerHost );
        foreach( var p in _packages )
        {
            p.ApplyChanges( c );
        }
    }


    internal static string NormalizeLogicalName( string logicalName )
    {
        return logicalName.Trim( '.' ).Replace( '/', '.' ).Replace( '\\', '.' );
    }

}
