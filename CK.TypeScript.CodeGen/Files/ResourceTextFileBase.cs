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
    /// Returns the resource stream only if the content has not been set or already loaded.
    /// </summary>
    /// <returns>The resource stream or null if <see cref="GetCurrentText()"/> is available.</returns>
    public override Stream? TryGetContentStream() => _content == null
                                                        ? _locator.GetStream()
                                                        : null;
}
