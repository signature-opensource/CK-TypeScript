using CK.Core;
using CK.EmbeddedResources;

namespace CK.Transform.Space;

/// <summary>
/// A package groups <see cref="TransformableSource"/> and are registered
/// in order in the <see cref="TransformSpace"/>.
/// </summary>
public sealed class TransformPackage
{
    readonly TransformSpace _space;
    readonly string _displayName;
    readonly int _index;
    TransformableSource? _firstDirty;

    internal TransformPackage( TransformSpace space, string displayName, int index )
    {
        _space = space;
        _displayName = displayName;
        _index = index;
    }

    public bool RegisterSource( IActivityMonitor monitor,
                                ResourceLocator origin,
                                NormalizedPath target,
                                out TransformableSource? result )
    {
        result = null;
        var host = _space._transformerHost;
        var p = origin.ResourceName.Span;
        var language = host.FindFromFilename( p );
        if( language == null )
        {
            monitor.Warn( $"No language found for file '{p}'." );
            return true;
        }
        // We want the local file path only if the container is a FileSystemResourceContainer.
        // When the origin is an AssemblyResourceContainer or a FileSystemResourceContainer with
        // no local file support, we consider the resource immutable.
        string? localFilePath = null;
        if( origin.Container is FileSystemResourceContainer fs && fs.HasLocalFilePathSupport )
        {
            localFilePath = origin.FullResourceName;
        }
        if( _space._items.TryGetValue( target, out var already )
            || (localFilePath != null && _space._localItems.TryGetValue( localFilePath, out already )) )
        {
            monitor.Error( $"Transformable item '{target}' cannot be registered from {origin}. It is already registered from {already.Origin}." );
            return false;
        }
        result = new TransformableSource( this, origin, target, language, localFilePath );
        _space._items.Add( target, result );
        if( localFilePath != null )
        {
            _space._localItems.Add( localFilePath, result );
        }
        return true;
    }

    internal void OnChange( IActivityMonitor monitor, TransformableSource source )
    {
        Throw.DebugAssert( source.Package == this && source.State != TransformableSourceState.Dirty );
        source.SetDirty();
        if( _firstDirty == null ) _firstDirty = source;
        else
        {
            source._nextDirty = _firstDirty;
            _firstDirty = source;
        }
    }

    internal void ApplyChanges( ApplyChangesContext c )
    {
        var s = _firstDirty;
        while( s != null )
        {
            Throw.DebugAssert( s.State == TransformableSourceState.Dirty );
            var t = s;
            s = t._nextDirty;
            t._nextDirty = null;
            t.ApplyChanges( c );
        }
    }

}
