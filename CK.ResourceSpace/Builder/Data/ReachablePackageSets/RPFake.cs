using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Fake set of packages created after all the real ones have been registered to 
/// be the missing binary nodes required to represent all sets. 
/// </summary>
sealed class RPFake : IReachablePackageSet
{
    readonly int _index;

    internal int _iSet1;
    internal int _iSet2;

    public RPFake( int index )
    {
        _index = index;
    }

    public int Index => _index;

    public int Count => Throw.NotSupportedException<int>();

    public bool Contains( ResPackage item ) => Throw.NotSupportedException<bool>();

    public IEnumerator<ResPackage> GetEnumerator() => Throw.NotSupportedException<IEnumerator<ResPackage>>()

    public bool IsProperSubsetOf( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    public bool IsProperSupersetOf( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    public bool IsSubsetOf( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    public bool IsSupersetOf( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    public bool Overlaps( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    public bool SetEquals( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
