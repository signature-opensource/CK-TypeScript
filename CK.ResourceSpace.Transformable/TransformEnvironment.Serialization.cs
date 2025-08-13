using CK.BinarySerialization;
using CK.Transform.Core;
using System.Collections.Generic;

namespace CK.Core;

sealed partial class TransformEnvironment
{
    public TransformEnvironment( ResCoreData spaceData, TransformerHost transformerHost, IBinaryDeserializer d )
    {
        _transformerHost = transformerHost;
        _items = d.ReadObject<Dictionary<NormalizedPath, TransformableItem>>();
        _transformFunctions = d.ReadObject<Dictionary<string, TFunction>>();
        _tracker = new StableItemOrInputTracker( spaceData, d );
        _functionSourceCollector = d.ReadObject<List<FunctionSource>>();
        _unboundFunctions = new HashSet<TFunction>();
    }

    internal void Serialize( IBinarySerializer s )
    {
        Throw.DebugAssert( _functionSourceCollector != null );
        s.WriteObject( _items );
        s.WriteObject( _transformFunctions );
        _tracker.Serialize( s );
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
