using CK.Core;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// <see cref="BaseFile.Extension"/> is ".html" or ".htm".
/// </summary>
public sealed class ResourceHtmlFile : ResourceTextFileBase
{
    internal ResourceHtmlFile( TypeScriptFolder folder, string name, in ResourceTypeLocator locator )
        : base( folder, name, in locator )
    {
    }
}


