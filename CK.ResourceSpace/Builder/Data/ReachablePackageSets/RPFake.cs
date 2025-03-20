using System.Collections.Generic;
using System.ComponentModel;

namespace CK.Core;

/// <summary>
/// Fake set of packages created after all the real ones have been registered.
/// They are the missing binary nodes required to represent all sets.
/// They are not sets but recipes: they state that to obtain the set <see cref="CacheIndex"/>,
/// aggregate <see cref="CIndex1"/> and <see cref="CIndex2"/>.
/// </summary>
[SerializationVersion(0)]
sealed class RPFake : IReachablePackageSet, IRPDerived, ICKVersionedBinarySerializable
{
    readonly int _cacheIndex;
    // IReachablePackageSet index is positive for stable set,
    // negative (biwise complement) for local ones.
    // This is the same encoding as the ResPackage index in the RPSetKey.
    readonly int _cIndex1;
    readonly int _cIndex2;

    public RPFake( IReachablePackageSet s1, IReachablePackageSet s2, int index )
    {
        _cIndex1 = s1.IsLocalDependent ? ~s1.CacheIndex : s1.CacheIndex;
        _cIndex2 = s2.IsLocalDependent ? ~s2.CacheIndex : s2.CacheIndex;
        if( _cIndex1 >= 0 && _cIndex2 < 0 )
        {
            (_cIndex1, _cIndex2) = (_cIndex2, _cIndex1);
        }
        _cacheIndex = index;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public RPFake( ICKBinaryReader r, int version )
    {
        _cacheIndex = r.ReadSmallInt32();
        _cIndex1 = r.ReadInt32();
        _cIndex2 = r.ReadInt32();
    }

    public void WriteData( ICKBinaryWriter w )
    {
        w.WriteNonNegativeSmallInt32( _cacheIndex );
        w.Write( _cIndex1 );
        w.Write( _cIndex2 );
    }


    public int CacheIndex => _cacheIndex;

    public bool IsLocalDependent => _cIndex1 < 0;

    public int CIndex1 => _cIndex1;

    public int CIndex2 => _cIndex2;

    public int Count => Throw.NotSupportedException<int>();

    public bool Contains( ResPackage item ) => Throw.NotSupportedException<bool>();

    public IEnumerator<ResPackage> GetEnumerator() => Throw.NotSupportedException<IEnumerator<ResPackage>>();

    public bool IsProperSubsetOf( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    public bool IsProperSupersetOf( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    public bool IsSubsetOf( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    public bool IsSupersetOf( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    public bool Overlaps( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    public bool SetEquals( IEnumerable<ResPackage> other ) => Throw.NotSupportedException<bool>();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
