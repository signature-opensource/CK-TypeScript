using CK.Core;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// <see cref="BaseFile.Extension"/> is ".js".
/// </summary>
public sealed class ResourceJavaScriptFile : ResourceTextFileBase
{
    internal ResourceJavaScriptFile( TypeScriptFolder folder, string name, in ResourceTypeLocator locator )
        : base( folder, name, in locator )
    {
    }
}

