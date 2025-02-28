using CK.Core;
using CK.Transform.Core;
using Microsoft.VisualBasic;
using System.Collections.Generic;

namespace CK.Transform.Space;


public sealed class TransformSpace
{
    readonly Dictionary<string, TransformPackage> _packageIndex;
    readonly List<TransformPackage> _packages;

    internal readonly Dictionary<string, TransformableSource> _items;
    internal readonly TransformerHost _transformerHost;

    public TransformSpace( params IEnumerable<TransformLanguage> languages )
    {
        _packageIndex = new Dictionary<string, TransformPackage>();
        _packages = new List<TransformPackage>();
        _items = new Dictionary<string, TransformableSource>();
        _transformerHost = new TransformerHost( languages );
    }

    public TransformPackage? RegisterPackage( IActivityMonitor monitor, string name )
    {
        if( _packageIndex.ContainsKey(name))
        {
            monitor.Error( $"Duplicate Transform package. Package '{name}' already exists." );
            return null;
        }
        var p = new TransformPackage( this, name, _packages.Count );
        _packages.Add( p );
        _packageIndex.Add( name, p );
        return p;
    }
}
