namespace CK.Core;

/// <summary>
/// Applies to all <see cref="IReachablePackageSet"/> implementations.
/// </summary>
interface IRPInternal
{
    int CacheIndex { get; }
    bool IsLocalDependent { get; }
}
