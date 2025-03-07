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
        _packageResources = s.ReadObject<IResourceContainer>();
        _codeGenResources = s.ReadObject<CodeGenResourceContainer>();
        _requires = s.ReadValue<ImmutableArray<ResPackage>>();
        _children = s.ReadValue<ImmutableArray<ResPackage>>();
        // Reacheable is the core set (deduplicated Requires + Requires' Children).
        // The reachable is rebuilt, not serialized. The difference is that we know its size upfront.
        _reachablePackages = new HashSet<ResPackage>( r.ReadNonNegativeSmallInt32() );
        (_requiresHasLocalPackage, _reachableHasLocalPackage, bool allRequired) = ComputeReachablePackages( _reachablePackages );

        int expectedSize = r.ReadSmallInt32();
        Throw.DebugAssert( (expectedSize >= 0) == allRequired );
        // AllReacheable. ComputeReachablePackages above computed the allRequired.
        if( allRequired )
        {
            _allReachablePackages = new HashSet<ResPackage>( expectedSize );
            _allReachableHasLocalPackage = ComputeAllReachablePackages( _allReachablePackages )
                                           || _reachableHasLocalPackage;
            Throw.DebugAssert( "allRequired should have been false!", _allReachablePackages.Count > _reachablePackages.Count );
        }
        else
        {
            _allReachablePackages = _reachablePackages;
            _allReachableHasLocalPackage = _reachableHasLocalPackage;
        }
    }

    public void Write( IBinarySerializer s )
    {
        var w = s.Writer;
        w.Write( _fullName );
        w.Write( _defaultTargetPath.Path );
        w.WriteNullableString( _localPath );
        w.Write( _isGroup );
        w.Write( _index );
        s.WriteObject( _packageResources );
        s.WriteObject( _codeGenResources );
        s.WriteValue( _requires );
        s.WriteValue( _children );
        w.WriteNonNegativeSmallInt32( _reachablePackages.Count );
        w.WriteSmallInt32( _allReachablePackages != _reachablePackages ? _allReachablePackages.Count : -1 );
    }
}
