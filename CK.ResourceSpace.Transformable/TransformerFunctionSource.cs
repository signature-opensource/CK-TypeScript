using CK.EmbeddedResources;

namespace CK.Core;

/// <summary>
/// A TransformerFunctionSource is a <see cref="TransformableSource"/> that defines
/// a set of <see cref="TransformerFunction"/>.
/// </summary>
sealed class TransformerFunctionSource : TransformableSource
{
    public TransformerFunctionSource( IResPackageResources resources, ResourceLocator origin )
        : base( resources, origin )
    {
    }
}
