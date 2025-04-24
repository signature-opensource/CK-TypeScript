using CK.BinarySerialization;
using CK.EmbeddedResources;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace CK.Core;

sealed partial class TransformEnvironment
{
    public TransformEnvironment( ResSpaceData spaceData, TransformerHost transformerHost, IBinaryDeserializer d )
    {
        _spaceData = spaceData;
        _transformerHost = transformerHost;
        _items = d.ReadObject<Dictionary<NormalizedPath, TransformableItem>>();
        _transformFunctions = d.ReadObject<Dictionary<string, TFunction>>();
        _functionSourceCollector = d.ReadObject<List<FunctionSource>>();
    }

    internal void Serialize( IBinarySerializer s )
    {
        Throw.DebugAssert( _functionSourceCollector != null );
        s.WriteObject( _items );
        s.WriteObject( _transformFunctions );
        s.WriteObject( _functionSourceCollector );
    }

    internal void PostDeserialization( IActivityMonitor monitor )
    {
        Throw.DebugAssert( _functionSourceCollector != null );
        foreach( var s in _functionSourceCollector )
        {
            s.PostDeserialization( monitor, this );
        }
        _functionSourceCollector = null;
    }

}
