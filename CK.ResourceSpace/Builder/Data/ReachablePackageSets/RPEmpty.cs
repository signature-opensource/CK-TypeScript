using CK.BinarySerialization;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Captures an empty reachable package set. This keeps the package that
/// defines the root reachable sets.
/// </summary>
[SerializationVersion( 0 )]
sealed class RPEmpty : IReachablePackageSet, IRPRoot, ICKSlicedSerializable
{
    readonly ResPackage _declarer;
    readonly int _cacheIndex;

    public RPEmpty( ResPackage declarer, int index )
    {
        _declarer = declarer;
        _cacheIndex = index;
    }

    public RPEmpty( IBinaryDeserializer d, ITypeReadInfo info )
    {
        _declarer = d.ReadObject<ResPackage>();
        _cacheIndex = d.Reader.ReadNonNegativeSmallInt32();
    }

    public static void Write( IBinarySerializer s, in RPEmpty o )
    {
        s.WriteObject( o._declarer );
        s.Writer.WriteNonNegativeSmallInt32( o._cacheIndex );
    }

    public int CacheIndex => _cacheIndex;

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
