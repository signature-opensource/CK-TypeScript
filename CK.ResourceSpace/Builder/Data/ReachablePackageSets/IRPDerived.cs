namespace CK.Core;

/// <summary>
/// Generalizes <see cref="RPSet"/> and <see cref="RPFake"/>.
/// </summary>
interface IRPDerived : IRPInternal
{
    int CIndex1 { get; }
    int CIndex2 { get; }
}
