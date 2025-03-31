using CK.BinarySerialization;
using CK.EmbeddedResources;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System;
using System.Xml.Linq;
using System.Diagnostics;
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
        _ckGenPath = r.ReadString();
        _ckWatchFolderPath = r.ReadString();
    }

    [EditorBrowsable( EditorBrowsableState.Never )]
    public void Write( IBinarySerializer s )
    {
        s.WriteValue( _localPackages );
        s.WriteValue( _packages );
        s.WriteValue( _allPackageResources );
        s.WriteObject( _packageIndex );

        ICKBinaryWriter w = s.Writer;
        ((ResPackageDataCache)_resPackageDataCache).Write( w );
        w.WriteNullableString( _watchRoot );
        w.Write( _ckGenPath );
        w.Write( _ckWatchFolderPath );
    }
}
