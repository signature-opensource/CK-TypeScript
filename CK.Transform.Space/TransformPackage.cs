using CK.Core;
using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace CK.Transform.Space;

/// <summary>
/// A package groups <see cref="TransformableSource"/> and are registered
/// in order in the <see cref="TransformSpace"/>.
/// </summary>
public sealed class TransformPackage
{
    readonly TransformSpace _space;
    readonly NormalizedPath _targetNamespace;
    readonly List<TransformerFunctions> _transformerFunctions;
    readonly int _index;
    TransformableSource? _firstDirty;

    public NormalizedPath TargetNamespace => _targetNamespace;

    internal TransformPackage( TransformSpace space, NormalizedPath targetNamespace, int index )
    {
        _space = space;
        _targetNamespace = targetNamespace;
        _transformerFunctions = new List<TransformerFunctions>();
        _index = index;
    }

    public bool RegisterTransform( IActivityMonitor monitor,
                                   ResourceLocator origin,
                                   out TransformerFunctions? result )
    {
        result = null;
        var host = _space._transformerHost;
        if( host.FindFromFilename( origin.Name )?.TransformLanguage.IsTransformerLanguage is not true )
        {
            Throw.ArgumentException( nameof( origin ), "Only transform file must be registered." );
        }
        var n = origin.ResourceName.Span;
        Throw.DebugAssert( Path.GetExtension( n ).Equals( ".t", StringComparison.Ordinal ) );
        n = n.Slice( 0, n.Length - 2 );
        var monoTargetLanguage = host.FindFromFilename( n );
        var transformTargetBase = n.ToString().Replace( '/', '.' ).Replace( '\\', '.' );
        result = new TransformerFunctions( this, origin, GetLocalFilePath( origin ), transformTargetBase, monoTargetLanguage );
        _transformerFunctions.Add( result );
        return true;
    }

    public bool RegisterItem( IActivityMonitor monitor,
                              ResourceLocator origin,
                              out TransformableItem? result,
                              NormalizedPath explicitTargetPath = default )
    {
        result = null;
        var originPath = origin.ResourceName.Span;
        if( explicitTargetPath.IsEmptyPath )
        {
            explicitTargetPath = _targetNamespace.Combine( origin.ResourceName.ToString() );
        }
        else
        {
            var ext = Path.GetExtension( origin.Name );
            if( !ext.Equals( Path.GetExtension( explicitTargetPath.LastPart.AsSpan() ), StringComparison.Ordinal ) )
            {
                monitor.Error( $"Invalid resource mapping for '{originPath}': extension '{ext}' cannot be changed by the target '{explicitTargetPath.LastPart}'." );
                return false;
            }
        }
        var host = _space._transformerHost;
        var language = host.FindFromFilename( originPath );
        if( language == null )
        {
            // When language is not recognized, the resource is unmanaged.
            // Unmanaged resources are not tracked by their localFileName at this level: the Live
            // tracks them from the changes in their folders.
            if( _space._unmanagedResources.TryGetValue( explicitTargetPath, out var alreadyUnmanaged ) )
            {
                monitor.Error( $"Unmanaged resource targeting '{explicitTargetPath}' cannot be registered from {origin}. It is already registered from {alreadyUnmanaged}." );
                return false;
            }
            Throw.DebugAssert( "Since we check the extension, no cross mapping is possible.",
                               !_space._items.ContainsKey( explicitTargetPath ) );
            _space._unmanagedResources.Add( explicitTargetPath, origin );
            return true;
        }
        if( language.TransformLanguage.IsTransformerLanguage )
        {
            Throw.ArgumentException( nameof( originPath ), "A transform file must not be registered as a transformable item." );
        }
        if( _space._items.TryGetValue( explicitTargetPath, out var already ) )
        {
            monitor.Error( $"Transformable item '{explicitTargetPath}' cannot be registered from {origin}. It is already registered from {already.Origin}." );
            return false;
        }
        result = new TransformableItem( this, origin, language, GetLocalFilePath( origin ), explicitTargetPath );
        _space._items.Add( explicitTargetPath, result );
        return true;
    }

    static string? GetLocalFilePath( ResourceLocator origin )
    {
        // We want the local file path only if the container is a FileSystemResourceContainer.
        // When the origin is an AssemblyResourceContainer or a FileSystemResourceContainer with
        // no local file support, we consider the resource immutable.
        string? localFilePath = null;
        if( origin.Container is FileSystemResourceContainer fs && fs.HasLocalFilePathSupport )
        {
            localFilePath = origin.FullResourceName;
        }

        return localFilePath;
    }

    internal void OnChange( IActivityMonitor monitor, TransformableSource source )
    {
        Throw.DebugAssert( source.Package == this && !source.IsDirty );
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
            Throw.DebugAssert( s.IsDirty );
            var t = s;
            s = t._nextDirty;
            t._nextDirty = null;
            t.ApplyChanges( c );
        }
    }

}
