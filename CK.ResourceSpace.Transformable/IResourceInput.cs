using CK.EmbeddedResources;

namespace CK.Core;

/// <summary>
/// Resource input: the <see cref="Origin"/> caches its <see cref="Text"/>
/// </summary>
interface IResourceInput
{
    IResPackageResources Resources { get; }
    ResourceLocator Origin { get; }
    string Text { get; }
}
