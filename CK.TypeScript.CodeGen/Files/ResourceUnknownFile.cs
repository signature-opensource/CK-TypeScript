using CK.EmbeddedResources;
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
    /// Always true.
    /// </summary>
    public override bool HasStream => true;

    /// <inheritdoc />
    public override Stream GetStream() => _locator.GetStream();

    /// <inheritdoc />
    public override void WriteStream( Stream target ) => _locator.WriteStream( target );

}


