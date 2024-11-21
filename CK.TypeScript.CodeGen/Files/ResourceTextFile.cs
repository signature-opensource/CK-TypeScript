using CK.Core;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// <see cref="BaseFile.Extension"/> is ".txt", ".text" or ".md".
/// </summary>
public sealed class ResourceTextFile : ResourceTextFileBase
{
    internal ResourceTextFile( TypeScriptFolder folder, string name, in ResourceLocator locator )
        : base( folder, name, in locator )
    {
    }
}


