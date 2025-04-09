using CK.BinarySerialization;
using CK.EmbeddedResources;
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
        _resourceIndex = d.ReadObject<IReadOnlyDictionary<IResourceContainer, IResPackageResources>>();

        _requiresHasLocalPackage = r.ReadBoolean();
        _reachableHasLocalPackage = r.ReadBoolean();
        _reachablePackages = d.ReadNullableObject<IReadOnlySet<ResPackage>>() ?? ImmutableHashSet<ResPackage>.Empty;
        _resources = d.ReadObject<BeforeRes>();
        _resourcesAfter = d.ReadObject<AfterRes>();

        int expectedSize = r.ReadNonNegativeSmallInt32();
        if( expectedSize > 0 )
        {
            var allReachablePackages = new HashSet<ResPackage>( expectedSize );
            _allReachableHasLocalPackage = ComputeAllReachablePackages( allReachablePackages )
                                           || _reachableHasLocalPackage;
            Throw.DebugAssert( "allRequired should have been false!", allReachablePackages.Count > _reachablePackages.Count );
            _allReachablePackages = allReachablePackages;
        }
        else
        {
            _allReachablePackages = _reachablePackages;
            _allReachableHasLocalPackage = _reachableHasLocalPackage;
        }

        if( _children.Length == 0 )
        {
            _afterReachablePackages = _reachablePackages;
            _afterReachableHasLocalPackage = _reachableHasLocalPackage;
            _allAfterReachablePackages = _allReachablePackages;
            _allAfterReachableHasLocalPackage = _allReachableHasLocalPackage;
        }
        else
        {
            _afterReachablePackages = d.ReadNullableObject<IReadOnlySet<ResPackage>>() ?? ImmutableHashSet<ResPackage>.Empty;
            expectedSize = r.ReadNonNegativeSmallInt32();
            var allAfterReachablePackages = new HashSet<ResPackage>( expectedSize );
            allAfterReachablePackages.AddRange( _allReachablePackages );
            (_childrenHasLocalPackage, _allAfterReachableHasLocalPackage) = ComputeAllAfterReachablePackage( allAfterReachablePackages );
            _allAfterReachableHasLocalPackage |= _allReachableHasLocalPackage;
            _allAfterReachablePackages = allAfterReachablePackages;
        }

        _reachableAggregateId = new AggregateId( r );
        _childrenAggregateId = new AggregateId( r );
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
        s.WriteObject( o._resourcesAfter );

        w.Write( o._requiresHasLocalPackage );
        w.Write( o._reachableHasLocalPackage );


        s.WriteNullableObject( o._reachablePackages != ImmutableHashSet<ResPackage>.Empty
                                ? o._reachablePackages
                                : null );
        Throw.DebugAssert( o._allReachablePackages == o._reachablePackages || o._allReachablePackages.Count > 1 );
        w.WriteNonNegativeSmallInt32( o._allReachablePackages != o._reachablePackages
                                            ? o._allReachablePackages.Count
                                            : 0 );

        if( o._children.Length > 0 )
        {
            s.WriteNullableObject( o._afterReachablePackages != ImmutableHashSet<ResPackage>.Empty
                                    ? o._afterReachablePackages
                                    : null );
            w.WriteNonNegativeSmallInt32( o._allAfterReachablePackages.Count );
        }

        o._reachableAggregateId.Write( w );
        o._childrenAggregateId.Write( w );
    }
}
