namespace CK.Core;

interface ITransformable
{
    TFunction? FirstFunction { get; }
    TFunction? LastFunction { get; }
    void Add( TFunction f );
    void Remove( TFunction f );
}
