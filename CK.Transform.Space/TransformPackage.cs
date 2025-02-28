using CK.Core;
using CK.EmbeddedResources;

namespace CK.Transform.Space;

public sealed class TransformPackage
{
    readonly TransformSpace _space;
    readonly string _displayName;
    readonly int _index;

    internal TransformPackage( TransformSpace space, string displayName, int index )
    {
        _space = space;
        _displayName = displayName;
        _index = index;
    }

    public bool RegisterSource( IActivityMonitor monitor, ResourceLocator origin, string logicalName, out TransformableSource? result )
    {
        result = null;
        var host = _space._transformerHost;
        var p = origin.LocalResourceName.Span;
        var language = host.FindFromFilename( p );
        if( language == null )
        {
            monitor.Warn( $"No language found for file '{p}'." );
            return true;
        }
        logicalName = NormalizeLogicalName( logicalName );
        if( _space._items.TryGetValue( logicalName, out var already ) )
        {
            monitor.Error( $"Transformable item '{logicalName}' cannot be registered from {origin}. It is already registered from {already.Origin}." );
            return false;
        }
        result = new TransformableSource( this, origin, logicalName, language );
        _space._items.Add( logicalName, result );
        return true;
    }

    static string NormalizeLogicalName( string logicalName )
    {
        return logicalName.Trim( '.' ).Replace( '/', '.' ).Replace( '\\', '.' );
    }
}
