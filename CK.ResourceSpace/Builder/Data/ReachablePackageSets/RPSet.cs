using CK.BinarySerialization;
using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Captures a set of at least 2 reachable packages.
/// Regardless of how many packages it contains, it is composed of 2 subsets
/// that can be <see cref="RPEmpty"/>, <see cref="RPSet"/> or <see cref="RPFake"/>.
/// Only their index are stored and serialized.
/// </summary>
[SerializationVersion( 0 )]
sealed class RPSet : HashSet<ResPackage>, IReachablePackageSet, IRPDerived, ICKSlicedSerializable
{
    int _cacheIndex;
    int _cIndex1;
    int _cIndex2;

    public int CacheIndex => _cacheIndex;

    public bool IsLocalDependent
    {
        get
        {
            Throw.DebugAssert( "Must not be called before initialization.", _cacheIndex >= 0 );
            return _cIndex1 < 0;
        }
    }

    public int CIndex1 => _cIndex1;

    public int CIndex2 => _cIndex2;

    internal void SetCachedIndex( int index )
    {
        Throw.DebugAssert( _cacheIndex == -1 );
        Throw.DebugAssert( index >= 0 );
        _cacheIndex = index;
    }

    internal void SettlePair( Dictionary<object, IReachablePackageSet> setIndex )
    {
        Throw.DebugAssert( Count == 2 );
        using var e = GetEnumerator();
        e.MoveNext();
        var s1 = setIndex[e.Current];
        e.MoveNext();
        var s2 = setIndex[e.Current];
        Settle( s1, s2 );
    }

    internal void Settle( IReachablePackageSet s1, IReachablePackageSet s2 )
    {
        Throw.DebugAssert( _cacheIndex >= 0 );
        Throw.DebugAssert( Count >= 2 );
        _cIndex1 = s1.IsLocalDependent ? ~s1.CacheIndex : s1.CacheIndex;
        _cIndex2 = s2.IsLocalDependent ? ~s2.CacheIndex : s2.CacheIndex;
        if( _cIndex1 >= 0 && _cIndex2 < 0 )
        {
            (_cIndex1, _cIndex2) = (_cIndex2, _cIndex1);
        }
    }

    public RPSet()
    {
        // Uninitialized.
        _cacheIndex = -1;
    }

    public RPSet( int capacity )
        : base( capacity )
    {
        // Uninitialized.
        _cacheIndex = -1;
    }

    public RPSet( IBinaryDeserializer d, ITypeReadInfo info )
    {
        Throw.DebugAssert( "We only serialize sets that are local dependent.", IsLocalDependent );
        _cacheIndex = d.Reader.ReadNonNegativeSmallInt32();
        int c = d.Reader.ReadNonNegativeSmallInt32();
        while( --c >= 0 )
        {
            Add( d.ReadObject<ResPackage>() );
        }
        _cIndex1 = d.Reader.ReadInt32();
        _cIndex2 = d.Reader.ReadInt32();
    }

    public static void Write( IBinarySerializer s, in RPSet o )
    {
        Throw.DebugAssert( "We are initialized.", o._cacheIndex >= 0 );
        Throw.DebugAssert( "We only serialize sets that are local dependent.", o.IsLocalDependent );
        s.Writer.WriteNonNegativeSmallInt32( o._cacheIndex );
        s.Writer.WriteNonNegativeSmallInt32( o.Count );
        foreach( var e in o )
        {
            s.WriteObject( e );
        }
        Throw.DebugAssert( "Because we are local dependent.", o._cIndex1 < 0 );
        s.Writer.Write( o._cIndex1 );
        s.Writer.Write( o._cIndex2 );
    }
}
