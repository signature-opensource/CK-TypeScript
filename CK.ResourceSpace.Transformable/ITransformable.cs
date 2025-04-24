using CK.Transform.Core;

namespace CK.Core;

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
