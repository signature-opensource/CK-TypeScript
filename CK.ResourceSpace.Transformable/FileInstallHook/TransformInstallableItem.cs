using CK.EmbeddedResources;
using CK.Transform.Core;

namespace CK.Core;

/// <summary>
/// Installable item handled by <see cref="ITransformableFileInstallHook"/>.
/// </summary>
public readonly struct TransformInstallableItem
{
    readonly TransformableItem _item;
    readonly TransformerHost.Language _language;

    internal TransformInstallableItem( TransformableItem item, TransformerHost host )
    {
        _item = item;
        _language = host.Languages[item.LanguageIndex];
    }

    /// <summary>
    /// Gets the resources that defines this item.
    /// </summary>
    public IResPackageResources Resources => _item.Resources;

    /// <summary>
    /// Gets the resource locator of this item.
    /// </summary>
    public ResourceLocator Origin => _item.Origin;

    /// <summary>
    /// Gets the install target path of this item.
    /// </summary>
    public NormalizedPath TargetPath => _item.TargetPath;

    /// <summary>
    /// Gets whether this item is a local item (not a stable one).
    /// </summary>
    public bool IsLocalItem => _item.IsLocalItem;

    /// <summary>
    /// Gets the item's language.
    /// </summary>
    public TransformerHost.Language Language => _language;
}

