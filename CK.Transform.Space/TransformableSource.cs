using CK.Core;
using CK.EmbeddedResources;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Space;

/// <summary>
/// Models a source file (a resource).
/// </summary>
public class TransformableSource
{
    readonly TransformPackage _package;
    readonly ResourceLocator _origin;

    internal TransformableSource? _nextDirty;

    // Text is loaded on demand and at most once per ApplyChanges.
    string? _text;
    int _getTextVersion;
    bool _isDirty;

    public TransformableSource( TransformPackage package,
                                ResourceLocator origin )
    {
        _package = package;
        _origin = origin;
    }

    public ResourceLocator Origin => _origin;

    public bool IsDirty => _isDirty;

    public TransformPackage Package => _package;

    public bool IsLocal => _origin.Container == _package.PackageResources && _package.LocalPath != null;

    internal void SetDirty()
    {
        Throw.DebugAssert( !IsDirty );
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
                if( c.CheckCanReadText( this ) )
                {
                    _text = _origin.ReadAsText();
                }
            }
            catch( Exception ex )
            {
                if( c.CheckCanReadText( this ) )
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
