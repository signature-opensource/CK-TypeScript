using CK.Core;
using System.IO;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Base class for all textual resources.
/// </summary>
public abstract class ResourceTextFileBase : TextFileBase, IResourceFile
{
    readonly ResourceLocator _locator;
    string? _content;

    internal ResourceTextFileBase( TypeScriptFolder folder, string name, in ResourceLocator locator )
        : base( folder, name )
    {
        _locator = locator;
    }

    /// <inheritdoc />
    public ResourceLocator Locator => _locator;

    /// <summary>
    /// Sets the textual content.
    /// Once set, the original resource is not used anymore.
    /// </summary>
    /// <param name="content">The textual content.</param>
    public void SetContent( string content )
    {
        Throw.CheckNotNullArgument( content );
        _content = content;
    }

    /// <inheritdoc />
    public override string GetCurrentText()
    {
        if( _content == null )
        {
            using var s = _locator.GetStream();
            _content = new StreamReader( s, detectEncodingFromByteOrderMarks: true, leaveOpen: true ).ReadToEnd();
        }
        return _content;
    }

    /// <summary>
    /// Gets whether textual content has not been already loaded or has been set.
    /// </summary>
    public override bool HasStream => _content == null;

    /// <inheritdoc />
    public override Stream GetStream()
    {
        Throw.CheckState( HasStream );
        return _locator.GetStream();
    }

    /// <inheritdoc />
    public override void WriteStream( Stream target )
    {
        Throw.CheckState( HasStream );
        _locator.WriteStream( target );
    }
}
