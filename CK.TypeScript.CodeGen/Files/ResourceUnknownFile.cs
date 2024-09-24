using CK.Core;
using System;
using System.IO;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Files for any type of resource for which no known extension has been found. 
/// </summary>
public sealed class ResourceUnknownFile : BaseFile, IResourceFile
{
    readonly ResourceTypeLocator _locator;

    internal ResourceUnknownFile( TypeScriptFolder folder, string name, in ResourceTypeLocator locator )
        : base( folder, name )
    {
        _locator = locator;
    }

    /// <inheritdoc />
    public ResourceTypeLocator Locator => _locator;

    public override Stream? TryGetContentStream() => GetContentStream();

    /// <summary>
    /// Provides the content of this file as a stream (that must be disposed once done with it).
    /// </summary>
    /// <returns>The content stream.</returns>
    public Stream GetContentStream() => _locator.GetStream();
}


