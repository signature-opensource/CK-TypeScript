using CK.BinarySerialization;
using System;
using System.Collections.Generic;

namespace CK.Core;

sealed partial class StableItemOrInputTracker
{
    public StableItemOrInputTracker( ResSpaceData spaceData, IBinaryDeserializer d )
    {
        _spaceData = spaceData;
        _o = d.ReadObject<object?[]>();
        _localChanges = new HashSet<object>?[spaceData.LocalPackageResources.Length];
    }

    internal void Serialize( IBinarySerializer s )
    {
        s.WriteObject( _o );
    }
}
