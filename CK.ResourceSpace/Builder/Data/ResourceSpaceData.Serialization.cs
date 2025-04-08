using CK.BinarySerialization;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;

namespace CK.Core;

[SerializationVersion(0)]
public sealed partial class ResourceSpaceData : ICKSlicedSerializable
{
    [EditorBrowsable( EditorBrowsableState.Never )]
    public ResourceSpaceData( IBinaryDeserializer d, ITypeReadInfo info )
    {
        _localPackages = d.ReadValue<ImmutableArray<ResPackage>>();
        _packages = d.ReadValue<ImmutableArray<ResPackage>>();
        _allPackageResources = d.ReadValue<ImmutableArray<IResPackageResources>>();
        _packageIndex = d.ReadObject<IReadOnlyDictionary<object, ResPackage>>();

        _codePackage = _packages[0];
        _appPackage = _packages[^1];
        _exposedPackages = new OneBasedArray( _packages );

        var r = d.Reader;
        _resPackageDataCache = LiveResPackageDataCache.Read( r, _packages );
        _watchRoot = r.ReadNullableString();
        _liveStatePath = r.ReadString();
    }

    [EditorBrowsable( EditorBrowsableState.Never )]
    public static void Write( IBinarySerializer s, in ResourceSpaceData o )
    {
        s.WriteValue( o._localPackages );
        s.WriteValue( o._packages );
        s.WriteValue( o._allPackageResources );
        s.WriteObject( o._packageIndex );

        ICKBinaryWriter w = s.Writer;
        ((ResPackageDataCache)o._resPackageDataCache).Write( w );
        w.WriteNullableString( o._watchRoot );
        w.Write( o._liveStatePath );
    }
}
