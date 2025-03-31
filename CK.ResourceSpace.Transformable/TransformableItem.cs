using CK.EmbeddedResources;
using CK.Transform.Core;

namespace CK.Core;

sealed class TransformableItem : TransformableSource
{
    readonly TransformerHost.Language _originLanguage;
    readonly NormalizedPath _target;
    string? _finalText;

    public TransformableItem( IResPackageResources resources,
                              ResourceLocator origin,
                              TransformerHost.Language originLanguage,
                              NormalizedPath target )
        : base( resources, origin )
    {
        _originLanguage = originLanguage;
        _target = target;
    }

    public NormalizedPath Target => _target;

    public string GetText( IActivityMonitor monitor )
    {
        if( _finalText == null )
        {
            _finalText = Origin.ReadAsText();
        }
        return _finalText;
    }

    public override string ToString() => _target;
}
