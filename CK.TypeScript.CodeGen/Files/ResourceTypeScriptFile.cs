using CK.EmbeddedResources;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// 
/// </summary>
public sealed class ResourceTypeScriptFile : TypeScriptFileBase
{
    readonly ResourceLocator _locator;

    internal ResourceTypeScriptFile( TypeScriptFolder folder, string name, in ResourceLocator locator, TypeScriptFileBase? previous )
        : base( folder, name, previous )
    {
        _locator = locator;
    }

    /// <summary>
    /// Gets the resource.
    /// </summary>
    public ResourceLocator Locator => _locator;

    /// <summary>
    /// Gets the resource textual content.
    /// </summary>
    /// <returns>This file's content.</returns>
    public override string GetCurrentText() => _locator.ReadAsText();
}


