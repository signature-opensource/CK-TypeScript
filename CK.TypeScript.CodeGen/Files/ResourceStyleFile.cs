using CK.Core;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// <see cref="BaseFile.Extension"/> is ".css" or ".less".
/// </summary>
public sealed class ResourceStyleFile : ResourceTextFileBase
{
    internal ResourceStyleFile( TypeScriptFolder folder, string name, in ResourceTypeLocator locator )
        : base( folder, name, in locator )
    {
    }
}


