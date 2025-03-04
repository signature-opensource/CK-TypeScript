using CK.Core;
using CK.EmbeddedResources;
using CK.Transform.Core;

namespace CK.Transform.Space;

public class TransformableItem : TransformableSource
{
    readonly NormalizedPath _target;
    readonly TransformerHost.Language _originLanguage;

    public TransformableItem( TransformPackage package,
                              ResourceLocator origin,
                              TransformerHost.Language originLanguage,
                              NormalizedPath target )
        : base( package, origin )
    {
        _originLanguage = originLanguage;
        _target = target;
    }

    public NormalizedPath Target => _target;
}
