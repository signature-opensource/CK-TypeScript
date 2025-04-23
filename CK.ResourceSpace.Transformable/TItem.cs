using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using static CK.Core.TFunction;

namespace CK.Core;

/// <summary>
/// A transformable item is a <see cref="TransformableSource"/> with a <see cref="TargetPath"/>. 
/// </summary>
sealed class TItem : TransformableSource, ITransformable
{
    readonly TransformerHost.Language _language;
    readonly NormalizedPath _target;
    TransformableImpl _transformableImpl;
    internal TItem? _nextInPackage;
    internal TItem? _prevInPackage;

    public TItem( IResPackageResources resources,
                  ResourceLocator origin,
                  TransformerHost.Language language,
                  NormalizedPath target,
                  string text )
        : base( resources, origin, text )
    {
        _language = language;
        _target = target;
    }

    public NormalizedPath TargetPath => _target;

    string ITransformable.TransfomableTargetName => _target.Path;


    public TransformerHost.Language Language => _language;

    TFunction? ITransformable.FirstFunction => _transformableImpl.FirstFunction;

    TFunction? ITransformable.LastFunction => _transformableImpl.LastFunction;

    bool ITransformable.TryFindInsertionPoint( IActivityMonitor monitor, TFunctionSource source, TransformerFunction f, out TFunction? before )
        => _transformableImpl.TryFindInsertionPoint( monitor, source, f, out before );

    void ITransformable.Add( TFunction f, TFunction? before ) => _transformableImpl.Add( f, before );

    void ITransformable.Remove( TFunction f ) => _transformableImpl.Remove( f );

    public override string ToString() => _target;

    internal string? GetFinalText( IActivityMonitor monitor, TransformerHost transformerHost )
    {
        Throw.DebugAssert( !IsShot );
        if( _transformableImpl.HasFunctions )
        {
            return _transformableImpl.Transform( monitor, transformerHost, Text.AsMemory() );
        }
        return Text;
    }
}
