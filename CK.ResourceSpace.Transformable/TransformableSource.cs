using CK.EmbeddedResources;

namespace CK.Core;

/// <summary>
/// Models a source file (a resource) that can be a final <see cref="TransformableItem"/>
/// or a <see cref="TransformerFunctionSource"/>.
/// </summary>
class TransformableSource
{
    readonly IResPackageResources _resources;
    readonly ResourceLocator _origin;

    public TransformableSource( IResPackageResources resources, ResourceLocator origin )
    {
        _resources = resources;
        _origin = origin;
    }

    public ResourceLocator Origin => _origin;
}
