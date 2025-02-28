using CK.EmbeddedResources;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Unifies <see cref="ResourceTextFileBase"/> and <see cref="ResourceUnknownFile"/>.
/// </summary>
public interface IResourceFile
{
    /// <summary>
    /// Gets the resource locator of this file.
    /// </summary>
    ResourceLocator Locator { get; }
}
