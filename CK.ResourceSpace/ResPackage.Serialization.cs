using CK.BinarySerialization;
using CK.EmbeddedResources;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Core;

[SerializationVersion(0)]
public sealed partial class ResPackage : ICKSlicedSerializable
{
    public ResPackage( IBinaryDeserializer s, ITypeReadInfo info )
    {
        var r = s.Reader;
        _fullName = r.ReadString();
        _defaultTargetPath = r.ReadString();
        _localPath = r.ReadNullableString();
        _isGroup = r.ReadBoolean();
        _index = r.ReadInt32();
        _requires = s.ReadValue<ImmutableArray<ResPackage>>();
        _children = s.ReadValue<ImmutableArray<ResPackage>>();

        _requiresHasLocalPackage = r.ReadBoolean();
        _reachableHasLocalPackage = r.ReadBoolean();
        _reachablePackages = s.ReadObject<IReachablePackageSet>();

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
            _afterReachablePackages = s.ReadObject<IReachablePackageSet>();
            expectedSize = r.ReadNonNegativeSmallInt32();
            var allAfterReachablePackages = new HashSet<ResPackage>( expectedSize );
            allAfterReachablePackages.AddRange( _allReachablePackages );
            (_childrenHasLocalPackage, _allAfterReachableHasLocalPackage) = ComputeAllContentReachablePackage( allAfterReachablePackages );
            _allAfterReachableHasLocalPackage |= _allReachableHasLocalPackage;
            _allAfterReachablePackages = allAfterReachablePackages;
        }
        // Initializes the resources once the package sets are computed.
        var bRes = new CodeStoreResources( s.ReadObject<IResourceContainer>(), s.ReadObject<IResourceContainer>() );
        _beforeResources = new BeforeContent( this, bRes, r.ReadNonNegativeSmallInt32() );

        var aRes = new CodeStoreResources( s.ReadObject<IResourceContainer>(), s.ReadObject<IResourceContainer>() );
        _afterResources = new AfterContent( this, aRes, r.ReadNonNegativeSmallInt32() );
    }

    public void Write( IBinarySerializer s )
    {
        var w = s.Writer;
        w.Write( _fullName );
        w.Write( _defaultTargetPath.Path );
        w.WriteNullableString( _localPath );
        w.Write( _isGroup );
        w.Write( _index );
        s.WriteValue( _requires );
        s.WriteValue( _children );

        w.Write( _requiresHasLocalPackage );
        w.Write( _reachableHasLocalPackage );

        s.WriteObject( _reachablePackages );
        Throw.DebugAssert( _allReachablePackages == _reachablePackages || _allReachablePackages.Count > 1 );
        w.WriteNonNegativeSmallInt32( _allReachablePackages != _reachablePackages
                                            ? _allReachablePackages.Count
                                            : 0 );

        if( _children.Length > 0 )
        {
            s.WriteObject( _afterReachablePackages );
            w.WriteNonNegativeSmallInt32( _allAfterReachablePackages.Count );
        }

        s.WriteObject( _beforeResources.Resources.Code );
        s.WriteObject( _beforeResources.Resources.Store );
        w.WriteNonNegativeSmallInt32( _beforeResources.Index );
        s.WriteObject( _afterResources.Resources.Code );
        s.WriteObject( _afterResources.Resources.Store );
        w.WriteNonNegativeSmallInt32( _afterResources.Index );
    }
}
