using CK.EmbeddedResources;

namespace CK.Core;

/// <summary>
/// Installable item handled by <see cref="ITransformableFileInstallHook"/>.
/// </summary>
public interface IInstallableItem
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
}

