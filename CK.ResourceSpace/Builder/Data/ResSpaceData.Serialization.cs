using CK.BinarySerialization;
using CK.EmbeddedResources;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;

namespace CK.Core;

[SerializationVersion(0)]
public sealed partial class ResSpaceData : ICKSlicedSerializable
{
    [EditorBrowsable( EditorBrowsableState.Never )]
    public ResSpaceData( IBinaryDeserializer d, ITypeReadInfo info )
    {
        _localPackages = d.ReadValue<ImmutableArray<ResPackage>>();
        _packages = d.ReadValue<ImmutableArray<ResPackage>>();
        _allPackageResources = d.ReadValue<ImmutableArray<IResPackageResources>>();
        _packageIndex = d.ReadObject<IReadOnlyDictionary<object, ResPackage>>();
        _resourceIndex = d.ReadObject<IReadOnlyDictionary<IResourceContainer, IResPackageResources>>();

        _codePackage = _packages[0];
        _appPackage = _packages[^1];

        var r = d.Reader;
        _resPackageDataCache = LiveSpaceDataCache.Read( r, _packages );
        _watchRoot = r.ReadNullableString();
        _liveStatePath = r.ReadString();
    }

    [EditorBrowsable( EditorBrowsableState.Never )]
    public static void Write( IBinarySerializer s, in ResSpaceData o )
    {
        s.WriteValue( o._localPackages );
        s.WriteValue( o._packages );
        s.WriteValue( o._allPackageResources );
        s.WriteObject( o._packageIndex );
        s.WriteObject( o._resourceIndex );

        ICKBinaryWriter w = s.Writer;
        ((SpaceDataCache)o._resPackageDataCache).Write( w );
        w.WriteNullableString( o._watchRoot );
        w.Write( o._liveStatePath );
    }
}
