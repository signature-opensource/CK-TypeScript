using CK.EmbeddedResources;

namespace CK.Core;

sealed class TransformerFunctions : TransformableSource
{
    public TransformerFunctions( IResPackageResources resources, ResourceLocator origin )
        : base( resources, origin )
    {
    }
}
