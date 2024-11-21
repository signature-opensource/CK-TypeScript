using CK.Core;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// <see cref="BaseFile.Extension"/> is ".html" or ".htm".
/// </summary>
public sealed class ResourceHtmlFile : ResourceTextFileBase
{
    internal ResourceHtmlFile( TypeScriptFolder folder, string name, in ResourceLocator locator )
        : base( folder, name, in locator )
    {
    }
}


