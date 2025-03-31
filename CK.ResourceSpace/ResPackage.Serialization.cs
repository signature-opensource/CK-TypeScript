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
        _localPath = r.ReadNullableString();
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
            (_childrenHasLocalPackage, _allAfterReachableHasLocalPackage) = ComputeAllContentReachablePackage( allAfterReachablePackages );
            _allAfterReachableHasLocalPackage |= _allReachableHasLocalPackage;
            _allAfterReachablePackages = allAfterReachablePackages;
        }

        _reachableAggregateId = new AggregateId( r );
        _childrenAggregateId = new AggregateId( r );
    }

    [EditorBrowsable( EditorBrowsableState.Never )]
    public void Write( IBinarySerializer s )
    {
        var w = s.Writer;
        w.Write( _fullName );
        s.WriteNullableObject( _type );
        w.Write( _defaultTargetPath.Path );
        w.WriteNullableString( _localPath );
        w.Write( _isGroup );
        w.Write( _index );
        s.WriteValue( _requires );
        s.WriteValue( _children );
        s.WriteObject( _resourceIndex );
        s.WriteObject( _resources );
        s.WriteObject( _resourcesAfter );

        w.Write( _requiresHasLocalPackage );
        w.Write( _reachableHasLocalPackage );


        s.WriteNullableObject( _reachablePackages != ImmutableHashSet<ResPackage>.Empty
                                ? _reachablePackages
                                : null );
        Throw.DebugAssert( _allReachablePackages == _reachablePackages || _allReachablePackages.Count > 1 );
        w.WriteNonNegativeSmallInt32( _allReachablePackages != _reachablePackages
                                            ? _allReachablePackages.Count
                                            : 0 );

        if( _children.Length > 0 )
        {
            s.WriteNullableObject( _afterReachablePackages != ImmutableHashSet<ResPackage>.Empty
                                    ? _afterReachablePackages
                                    : null );
            w.WriteNonNegativeSmallInt32( _allAfterReachablePackages.Count );
        }

        _reachableAggregateId.Write( w );
        _childrenAggregateId.Write( w );
    }
}
