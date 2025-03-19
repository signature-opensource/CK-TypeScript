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
sealed class RPSet : HashSet<ResPackage>, IReachablePackageSet, ICKSlicedSerializable
{
    internal int _index;

    IReachablePackageSet? _iSet1;
    IReachablePackageSet? _iSet2;

    public int Index => _index;

    public bool IsLocalDependent
    {
        get
        {
            Throw.DebugAssert( "Must not be called before initialization.", _iSet1 != null );
            return _iSet1.IsLocalDependent;
        }
    }

    internal void InitializePair( Dictionary<object, IReachablePackageSet> index )
    {
        Throw.DebugAssert( Count == 2 );
        using var e = GetEnumerator();
        e.MoveNext();
        _iSet1 = index[e.Current];
        e.MoveNext();
        _iSet2 = index[e.Current];
        if( !_iSet1.IsLocalDependent && _iSet2.IsLocalDependent )
        {
            (_iSet1, _iSet2) = (_iSet2, _iSet1);
        }
    }

    public RPSet()
    {
    }

    public RPSet( int capacity )
        : base( capacity )
    {
    }

    public RPSet( IBinaryDeserializer d, ITypeReadInfo info )
    {
        _index = d.Reader.ReadInt32();
        int c = d.Reader.ReadInt32();
        while( --c >= 0 )
        {
            Add( d.ReadObject<ResPackage>() );
        }
    }

    public static void Write( IBinarySerializer s, in RPSet o )
    {
        s.Writer.Write( o._index );
        s.Writer.Write( o.Count );
        foreach( var e in o )
        {
            s.WriteObject( e );
        }
    }
}
