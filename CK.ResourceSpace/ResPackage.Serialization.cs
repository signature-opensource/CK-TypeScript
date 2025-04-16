using CK.BinarySerialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;

namespace CK.Core;

[SerializationVersion(0)]
public sealed partial class ResPackage : ICKSlicedSerializable
{
    [EditorBrowsable( EditorBrowsableState.Never )]
    public ResPackage( IBinaryDeserializer d, ITypeReadInfo info )
    {
        var r = d.Reader;
        _fullName = r.ReadString();
        _type = d.ReadNullableObject<Type>();
        _defaultTargetPath = r.ReadString();
        _isGroup = r.ReadBoolean();
        _index = r.ReadInt32();
        _requires = d.ReadValue<ImmutableArray<ResPackage>>();
        _children = d.ReadValue<ImmutableArray<ResPackage>>();
        _spaceData = d.ReadObject<ResSpaceData>();
        _resources = d.ReadObject<BeforeRes>();
        _afterResources = d.ReadObject<AfterRes>();

        // Note: we cannot check any invariant that consider other packages because
        //       we are in the middle of the serialized graph.
        if( _requires.Length == 0 )
        {
            _reachables = ImmutableHashSet<ResPackage>.Empty;
        }
        else
        {
            _reachables = d.ReadObject<IReadOnlySet<ResPackage>>();
            _requiresAggregateId = new AggregateId( r );
        }

        if( _children.Length == 0 )
        {
            _afterReachables = _reachables;
        }
        else
        {
            _afterReachables = d.ReadObject<IReadOnlySet<ResPackage>>();
            _childrenAggregateId = new AggregateId( r );
        }
    }

    [EditorBrowsable( EditorBrowsableState.Never )]
    public static void Write( IBinarySerializer s, in ResPackage o )
    {
        var w = s.Writer;
        w.Write( o._fullName );
        s.WriteNullableObject( o._type );
        w.Write( o._defaultTargetPath.Path );
        w.Write( o._isGroup );
        w.Write( o._index );
        s.WriteValue( o._requires );
        s.WriteValue( o._children );
        s.WriteObject( o._spaceData );

        s.WriteObject( o._resources );
        s.WriteObject( o._afterResources );

        if( o._requires.Length > 0 )
        {
            s.WriteObject( o._reachables );
            o._requiresAggregateId.Write( w );
        }

        if( o._children.Length > 0 )
        {
            s.WriteObject( o._afterReachables );
            o._childrenAggregateId.Write( w );
        }
    }
}
