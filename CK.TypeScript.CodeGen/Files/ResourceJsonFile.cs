using CK.EmbeddedResources;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// <see cref="BaseFile.Extension"/> is ".json" or ".jsonc".
/// </summary>
public sealed class ResourceJsonFile : ResourceTextFileBase
{
    internal ResourceJsonFile( TypeScriptFolder folder, string name, in ResourceLocator locator )
        : base( folder, name, in locator )
    {
    }
}

