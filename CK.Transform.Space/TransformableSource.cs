using CK.Core;
using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CK.Transform.Space;

/// <summary>
/// Models a source resource.
/// </summary>
public sealed class TransformableSource
{
    readonly TransformPackage _package;
    readonly ResourceLocator _origin;
    readonly NormalizedPath _target;
    readonly TransformerHost.Language _originLanguage;
    readonly string? _localFilePath;

    internal TransformableSource? _nextDirty;

    // Text is loaded on demand and at most once per ApplyChanges.
    string? _text;
    int _getTextVersion;
    // When the origin language is the Transfomer, this contains
    // the parsed transfomers on success.
    List<TransformerFunction>? _transformers;
    TransformableSourceState _state;

    public TransformableSource( TransformPackage package,
                                ResourceLocator origin,
                                NormalizedPath target,
                                TransformerHost.Language originLanguage,
                                string? localFilePath )
    {
        _package = package;
        _origin = origin;
        _target = target;
        _originLanguage = originLanguage;
        _localFilePath = localFilePath;
    }

    public ResourceLocator Origin => _origin;

    public string LogicalName => _target;

    public TransformableSourceState State => _state;

    public TransformPackage Package => _package;

    public string? LocalFilePath => _localFilePath;

    internal void SetDirty()
    {
        Throw.DebugAssert( _state != TransformableSourceState.Dirty );
        _state = TransformableSourceState.Dirty;
        _text = null;
    }

    internal void ApplyChanges( ApplyChangesContext c )
    {
        Throw.DebugAssert( _text == null );
        if( TryGetText( c, out var text )
            && _originLanguage.TransformLanguage.IsTransformerLanguage )
        {
            _transformers = c.Host.TryParseFunctions( c.Monitor, text );
            if( _transformers == null )
            {
                c.AddError( $"Unable to parse transformers from {_origin}." );
            }
            else if( _transformers.Count == 0 )
            {
                c.Monitor.Warn( $"No transformers found in {_origin}." );
            }
            else
            {
                foreach( var t in _transformers )
                {
                    _origin.ResourceName
                }
            }
        }
    }

    bool TryGetText( ApplyChangesContext c, [NotNullWhen(true)]out string? text )
    {
        if( _text == null && _getTextVersion != c.Version )
        {
            _getTextVersion = c.Version;
            try
            {
                if( _localFilePath == null || c.LocalFileExists( this ) )
                {
                    _text = _origin.ReadAsText();
                }
            }
            catch( Exception ex )
            {
                if( _localFilePath == null || c.LocalFileExists( this ) )
                {
                    c.AddError( $"While reading {_origin}.", ex );
                }
            }
        }
        return (text = _text) != null;
    }
}
