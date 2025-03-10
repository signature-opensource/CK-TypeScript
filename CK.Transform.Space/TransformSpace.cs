using CK.Core;
using CK.EmbeddedResources;
using CK.Transform.Core;
using System.Collections.Generic;

namespace CK.Transform.Space;


public sealed class TransformSpace
{
    readonly Dictionary<string, TransformPackage> _packageIndex;
    readonly List<TransformPackage> _packages;

    // All items are indexed by their target path.
    internal readonly Dictionary<string, TransformableItem> _items;
    // Resources with unrecognized language (unknown extensions) are indexed by their
    // target path.
    internal readonly Dictionary<string, ResourceLocator> _unmanagedResources;
    // Live tracking: FileSystem local files are tracked.
    internal readonly Dictionary<string, TransformableSource> _localFiles;
    // Live tracking: LocalPackages only.
    internal readonly List<TransformPackage> _localPackages;

    internal readonly TransformerHost _transformerHost;

    int _applyChangesVersion;

    public TransformSpace( params IEnumerable<TransformLanguage> languages )
    {
        _packageIndex = new Dictionary<string, TransformPackage>();
        _packages = new List<TransformPackage>();
        _items = new Dictionary<string, TransformableItem>();
        _unmanagedResources = new Dictionary<string, ResourceLocator>();
        _localFiles = new Dictionary<string, TransformableSource>();
        _localPackages = new List<TransformPackage>();
        _transformerHost = new TransformerHost( languages );
    }

    public TransformPackage? RegisterPackage( IActivityMonitor monitor,
                                              string name,
                                              NormalizedPath defaultTargetPath,
                                              IResourceContainer packageResources )
    {
        if( _packageIndex.ContainsKey( name ) )
        {
            monitor.Error( $"Duplicate Transform package. Package '{name}' already exists." );
            return null;
        }
        string? localPath = packageResources is FileSystemResourceContainer fs && fs.HasLocalFilePathSupport
                                ? fs.ResourcePrefix
                                : null;
        var p = new TransformPackage( this, name, defaultTargetPath, packageResources, localPath, _packages.Count );
        _packages.Add( p );
        _packageIndex.Add( name, p );
        if( localPath != null )
        {
            _localPackages.Add( p );
        }
        return p;
    }

    public void OnChange( IActivityMonitor monitor, string localFilePath )
    {
        if( _localFiles.TryGetValue( localFilePath, out var source ) )
        {
            if( !source.IsDirty )
            {
                source.Package.OnTrackedChange( monitor, source );
            }
        }
        else
        {
            foreach( var p in _localPackages )
            {
                Throw.DebugAssert( p.LocalPath != null );
                if( localFilePath.StartsWith( p.LocalPath ) )
                {
                    p.OnUntrackedChange( monitor, localFilePath );
                }
            }
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

}
