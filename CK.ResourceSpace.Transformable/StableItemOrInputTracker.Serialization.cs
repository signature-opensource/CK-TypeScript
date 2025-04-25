using CK.BinarySerialization;
using System;

namespace CK.Core;

sealed partial class StableItemOrInputTracker
{
    public StableItemOrInputTracker( ResSpaceData spaceData, IBinaryDeserializer d )
    {
        _spaceData = spaceData;
        _o = d.ReadObject<object?[]>();
    }

    internal void Serialize( IBinarySerializer s )
    {
        s.WriteObject( _o );
    }
}
