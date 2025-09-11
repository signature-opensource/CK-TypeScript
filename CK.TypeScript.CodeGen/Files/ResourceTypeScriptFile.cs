using CK.Core;
using CK.EmbeddedResources;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// A TypeScript file that belongs to a resource container.
/// <see cref="ITSDeclaredFileType"/> can be declared for it (so it can be resolved) but the
/// source text is not under control of the code generator.
/// </summary>
public sealed class ResourceTypeScriptFile : TypeScriptFileBase
{
    readonly ResourceLocator _locator;
    readonly bool _isPublishedResource;

    internal ResourceTypeScriptFile( TypeScriptFolder folder,
                                     string name,
                                     ResourceLocator locator,
                                     TypeScriptFileBase? previous,
                                     bool isPublishedResource )
        : base( folder, name, previous, !isPublishedResource )
    {
        _locator = locator;
        _isPublishedResource = isPublishedResource;
    }

    /// <summary>
    /// Gets the resource.
    /// </summary>
    public ResourceLocator Locator => _locator;

    /// <summary>
    /// Gets whether this resource must be published in the Code Generated Container.
    /// See <see cref="ITypeScriptPublishTarget"/>.
    /// </summary>
    public bool IsPublishedResource => _isPublishedResource;

    /// <inheritdoc />
    public override string GetCurrentText( IActivityMonitor monitor, TSTypeManager tSTypes ) => _locator.ReadAsText();
}


