using CK.Core;
using System.IO;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Files for any type of resource for which no known extension has been found. 
/// </summary>
public sealed class ResourceUnknownFile : BaseFile, IResourceFile
{
    readonly ResourceLocator _locator;

    internal ResourceUnknownFile( TypeScriptFolder folder, string name, in ResourceLocator locator )
        : base( folder, name )
    {
        _locator = locator;
    }

    /// <inheritdoc />
    public ResourceLocator Locator => _locator;

    /// <summary>
    /// Returns <see cref="GetContentStream()"/>.
    /// </summary>
    /// <returns>The stream.</returns>
    public override Stream TryGetContentStream() => GetContentStream();

    /// <summary>
    /// Provides the content of this file as a stream (that must be disposed once done with it).
    /// </summary>
    /// <returns>The content stream.</returns>
    public Stream GetContentStream() => _locator.GetStream();
}


