using CK.EmbeddedResources;

namespace CK.Core;

/// <summary>
/// Provided context to the <see cref="ResPackageDescriptor"/>.
/// </summary>
interface IResPackageDescriptorContext
{
    void RegisterCodeHandledResources( ResourceLocator resource );
}
