using CK.BinarySerialization;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Captures an empty reachable package set. This keeps the package that
/// defines the reachable set: <see cref="ReachablePackageDictionary{T}.Create(CK.Core.IActivityMonitor, CK.Core.ResPackage)"/>
/// can use it to create the aggregated associated data seed.
/// </summary>
[SerializationVersion( 0 )]
sealed class RPEmpty : IReachablePackageSet, IRPRoot, ICKSlicedSerializable
{
    readonly ResPackage _declarer;
    readonly int _index;

    public RPEmpty( ResPackage declarer, int index )
    {
        _declarer = declarer;
        _index = index;
    }

    public int Index => _index;

    public bool IsLocalDependent => _declarer.IsEventuallyLocalDependent;

    public int Count => 0;

    public ResPackage RootPackage => _declarer;

    public bool Contains( ResPackage item ) => false;

    public IEnumerator<ResPackage> GetEnumerator() => Enumerable.Empty<ResPackage>().GetEnumerator();

    public bool IsProperSubsetOf( IEnumerable<ResPackage> other )
    {
        Throw.CheckNotNullArgument( other );
        return other.Any();
    }

    public bool IsProperSupersetOf( IEnumerable<ResPackage> other ) => false;

    public bool IsSubsetOf( IEnumerable<ResPackage> other ) => true;

    public bool IsSupersetOf( IEnumerable<ResPackage> other )
    {
        Throw.CheckNotNullArgument( other );
        return !other.Any();
    }

    public bool Overlaps( IEnumerable<ResPackage> other ) => false;

    public bool SetEquals( IEnumerable<ResPackage> other ) => IsSupersetOf( other );

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
