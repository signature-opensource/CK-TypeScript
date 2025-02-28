using CK.EmbeddedResources;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// <see cref="BaseFile.Extension"/> is ".css" or ".less".
/// </summary>
public sealed class ResourceStyleFile : ResourceTextFileBase
{
    internal ResourceStyleFile( TypeScriptFolder folder, string name, in ResourceLocator locator )
        : base( folder, name, in locator )
    {
    }
}


