using CK.Core;
using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CK.Transform.Space;

/// <summary>
/// Models a source file (a resource).
/// </summary>
public class TransformableSource
{
    readonly TransformPackage _package;
    readonly ResourceLocator _origin;
    readonly string? _localFilePath;

    internal TransformableSource? _nextDirty;

    // Text is loaded on demand and at most once per ApplyChanges.
    string? _text;
    int _getTextVersion;
    bool _isDirty;

    public TransformableSource( TransformPackage package,
                                ResourceLocator origin,
                                string? localFilePath )
    {
        _package = package;
        _origin = origin;
        _localFilePath = localFilePath;
    }

    public ResourceLocator Origin => _origin;

    public bool IsDirty => _isDirty;

    public TransformPackage Package => _package;

    public string? LocalFilePath => _localFilePath;

    internal void SetDirty()
    {
        Throw.DebugAssert( !IsDirty && _localFilePath != null );
        _isDirty = true;
        _text = null;
    }

    internal void ApplyChanges( ApplyChangesContext c )
    {
        Throw.DebugAssert( _text == null );
        TryGetText( c, out var text );
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
        if( (text = _text) == null )
        {
            return false;
        }
        if( OnTextAvailable( c, text ) )
        {
            return true;
        }
        Throw.DebugAssert( c.HasError );
        _text = null;
        return false;
    }

    private protected virtual bool OnTextAvailable( ApplyChangesContext c, string text ) => true;
}
