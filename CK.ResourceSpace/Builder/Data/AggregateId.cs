using System;

namespace CK.Core;

readonly struct AggregateId : IEquatable<AggregateId>, ICKSimpleBinarySerializable
{
    internal readonly int _localKeyId;
    internal readonly int _stableKeyId;

    public AggregateId( int localKeyId, int stableKeyId )
    {
        _localKeyId = localKeyId;
        _stableKeyId = stableKeyId;
    }

    public bool Equals( AggregateId other ) => _localKeyId == other._localKeyId && _stableKeyId == other._stableKeyId;

    public override bool Equals( object? obj ) => obj is AggregateId id && Equals( id );

    public static bool operator ==( AggregateId a1, AggregateId a2 ) => a1.Equals( a2 );

    public static bool operator !=( AggregateId a1, AggregateId a2 ) => !a1.Equals( a2 );

    public override int GetHashCode() => HashCode.Combine( _localKeyId, _stableKeyId );

    public AggregateId( ICKBinaryReader r )
    {
        _localKeyId = r.ReadInt32();
        _stableKeyId = r.ReadInt32();
    }

    public void Write( ICKBinaryWriter w )
    {
        w.Write( _localKeyId );
        w.Write( _stableKeyId );
    }
}
