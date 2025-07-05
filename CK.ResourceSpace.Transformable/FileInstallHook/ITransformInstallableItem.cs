using CK.EmbeddedResources;
using CK.Transform.Core;

namespace CK.Core;

/// <summary>
/// Installable item handled by <see cref="ITransformableFileInstallHook"/>.
/// </summary>
public interface ITransformInstallableItem
{
    /// <summary>
    /// Gets the resources that defines this item.
    /// </summary>
    IResPackageResources Resources { get; }

    /// <summary>
    /// Gets the resource locator of this item.
    /// </summary>
    ResourceLocator Origin { get; }

    /// <summary>
    /// Gets the install target path of this item.
    /// </summary>
    NormalizedPath TargetPath { get; }

    /// <summary>
    /// Gets whether this item is a local item (not a stable one).
    /// </summary>
    bool IsLocalItem { get; }

    /// <summary>
    /// Gets the item's language index in the <see cref="TransformerHost.Languages"/>.
    /// </summary>
    public int LanguageIndex { get; }
}

