using CK.Core;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// <see cref="BaseFile.Extension"/> is ".json" or ".jsonc".
/// </summary>
public sealed class ResourceJsonFile : ResourceTextFileBase
{
    internal ResourceJsonFile( TypeScriptFolder folder, string name, in ResourceTypeLocator locator )
        : base( folder, name, in locator )
    {
    }
}

