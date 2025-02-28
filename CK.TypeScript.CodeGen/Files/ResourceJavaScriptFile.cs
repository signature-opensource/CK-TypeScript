using CK.EmbeddedResources;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// <see cref="BaseFile.Extension"/> is ".js".
/// </summary>
public sealed class ResourceJavaScriptFile : ResourceTextFileBase
{
    internal ResourceJavaScriptFile( TypeScriptFolder folder, string name, in ResourceLocator locator )
        : base( folder, name, in locator )
    {
    }
}

