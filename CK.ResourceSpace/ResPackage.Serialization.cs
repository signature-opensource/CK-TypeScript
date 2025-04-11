using CK.BinarySerialization;
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;

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
        _resourceIndex = d.ReadObject<IReadOnlyDictionary<IResourceContainer, IResPackageResources>>();
        _resources = d.ReadObject<BeforeRes>();
        _afterResources = d.ReadObject<AfterRes>();

        if( _requires.Length == 0 )
        {
            _reachables = ImmutableHashSet<ResPackage>.Empty;
            Throw.DebugAssert( _requiresAggregateId == default );
        }
        else
        {
            _reachables = d.ReadObject<IReadOnlySet<ResPackage>>();
            _requiresAggregateId = new AggregateId( r );
            Throw.DebugAssert( _requiresAggregateId != default
                               && _requiresAggregateId.HasLocal == _reachables.Any( r => r.IsEventuallyLocalDependent ) );
        }

        if( _children.Length == 0 )
        {
            _afterReachables = _reachables;
            Throw.DebugAssert( _childrenAggregateId == default );
        }
        else
        {
            _afterReachables = d.ReadObject<IReadOnlySet<ResPackage>>();
            _childrenAggregateId = new AggregateId( r );
            Throw.DebugAssert( _childrenAggregateId != default
                              && _childrenAggregateId.HasLocal == _children.SelectMany( c => c.AfterReachables ).Concat( _children )
                                                                           .Any( r => r.IsEventuallyLocalDependent ) );
        }
        Throw.DebugAssert( _afterReachables == _reachables || _afterReachables.IsProperSupersetOf( _reachables ) );
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
        s.WriteObject( o._resourceIndex );

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
