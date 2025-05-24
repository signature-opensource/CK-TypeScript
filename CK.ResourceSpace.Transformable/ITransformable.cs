using CK.Transform.Core;

namespace CK.Core;

/// <summary>
/// A <see cref="ITransformable"/> is a <see cref="TFunction"/>, a <see cref="TransformableItem"/>
/// or a <see cref="ExternalItem"/>.
/// </summary>
interface ITransformable
{
    string TransfomableTargetName { get; }

    TFunction? FirstFunction { get; }

    TFunction? LastFunction { get; }

    bool TryFindInsertionPoint( IActivityMonitor monitor,
                                FunctionSource source,
                                TransformerFunction f,
                                out TFunction? before );

    void Add( TFunction f, TFunction? before );

    void Remove( TFunction f );
}
