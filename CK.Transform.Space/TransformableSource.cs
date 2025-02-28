using CK.EmbeddedResources;
using CK.Transform.Core;

namespace CK.Transform.Space;

public sealed class TransformableSource
{
    readonly TransformPackage _package;
    readonly ResourceLocator _origin;
    readonly string _logicalName;
    readonly TransformerHost.Language _originLanguage;

    public TransformableSource( TransformPackage package,
                                ResourceLocator origin,
                                string logicalName,
                                TransformerHost.Language originLanguage )
    {
        _package = package;
        _origin = origin;
        _logicalName = logicalName;
        _originLanguage = originLanguage;
    }

    public ResourceLocator Origin => _origin;

    public string LogicalName => _logicalName;
}
