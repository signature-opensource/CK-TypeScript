using CK.Core;
using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace CK.Transform.Space;

/// <summary>
/// A package groups <see cref="TransformableSource"/> and are registered
/// in order in the <see cref="TransformSpace"/>.
/// </summary>
public sealed class TransformPackage
{
    readonly TransformSpace _space;
    readonly string _fullName;
    readonly NormalizedPath _defaultTargetPath;
    readonly IResourceContainer _packageResources;
    readonly List<TransformerFunctions> _transformerFunctions;
    readonly string? _localPath;
    readonly int _index;
    TransformableSource? _firstDirty;

    public string FullName => _fullName;

    public NormalizedPath TargetNamespace => _defaultTargetPath;

    public IResourceContainer PackageResources => _packageResources;

    public string? LocalPath => _localPath;

    internal TransformPackage( TransformSpace space,
                               string fullName,
                               NormalizedPath defaultTargetPath,
                               IResourceContainer packageResources,
                               string? localPath,
                               int index )
    {
        _space = space;
        _fullName = fullName;
        _defaultTargetPath = defaultTargetPath;
        _packageResources = packageResources;
        _localPath = localPath;
        _transformerFunctions = new List<TransformerFunctions>();
        _index = index;
    }

    bool Register( IActivityMonitor monitor, ResourceLocator origin )
    {
        TransformableSource? result;

        var host = _space._transformerHost;
        var language = host.FindFromFilename( origin.Name );
        if( language != null && language.TransformLanguage.IsTransformerLanguage )
        {
            var n = origin.ResourceName;
            Throw.DebugAssert( Path.GetExtension( n ).Equals( ".t", StringComparison.Ordinal ) );
            n = n.Slice( 0, n.Length - 2 );
            var monoTargetLanguage = host.FindFromFilename( n );
            var transformTargetBase = n.ToString().Replace( '/', '.' ).Replace( '\\', '.' );
            string? localFilePath = _localPath != null ? origin.FullResourceName : null;
            var functions = new TransformerFunctions( this, origin, transformTargetBase, monoTargetLanguage );
            _transformerFunctions.Add( functions );
            result = functions;
        }
        else
        {
            // Register non-transformer item.
            var originPath = origin.ResourceName;
            var targetPath = _defaultTargetPath.Combine( origin.ResourceName.ToString() );
            if( language == null )
            {
                // When language is not recognized, the resource is unmanaged.
                // Unmanaged resources are not tracked by their localFileName at this level: the Live
                // tracks them from the changes in their folders.
                if( _space._unmanagedResources.TryGetValue( targetPath, out var alreadyUnmanaged ) )
                {
                    monitor.Error( $"Unmanaged resource targeting '{targetPath}' cannot be registered from {origin}. It is already registered from {alreadyUnmanaged}." );
                    return false;
                }
                Throw.DebugAssert( "Since we check the extension, no cross mapping is possible.",
                                   !_space._items.ContainsKey( targetPath ) );
                _space._unmanagedResources.Add( targetPath, origin );
                result = null;
            }
            else
            {
                if( language.TransformLanguage.IsTransformerLanguage )
                {
                    Throw.ArgumentException( nameof( originPath ), "A transform file must not be registered as a transformable item." );
                }
                if( _space._items.TryGetValue( targetPath, out var already ) )
                {
                    monitor.Error( $"Transformable item '{targetPath}' cannot be registered from {origin}. It is already registered from {already.Origin}." );
                    return false;
                }
                var item = new TransformableItem( this, origin, language, targetPath );
                _space._items.Add( targetPath, item );
                result = item;
            }
        }
        if( result != null && result.IsLocal )
        {
            _space._localFiles.Add( origin.FullResourceName, result );
        }
        return true;
    }

    internal void OnTrackedChange( IActivityMonitor monitor, TransformableSource source )
    {
        Throw.DebugAssert( source.Package == this && source.IsLocal && !source.IsDirty );
        source.SetDirty();
        if( _firstDirty == null ) _firstDirty = source;
        else
        {
            source._nextDirty = _firstDirty;
            _firstDirty = source;
        }
    }

    internal void OnUntrackedChange( IActivityMonitor monitor, string localFilePath )
    {
        Throw.DebugAssert( _packageResources is FileSystemResourceContainer && _localPath != null );
        var fs = Unsafe.As<FileSystemResourceContainer>( _packageResources );
        var res = fs.GetResourceFromLocalPath( localFilePath );
        if( res.IsValid )
        {
            Register( monitor, res );
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
