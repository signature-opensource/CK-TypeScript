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
        _sources = d.ReadObject<Dictionary<ResourceLocator, TransformableSource>>();
        _items = d.ReadObject<Dictionary<NormalizedPath, TItem>>();
        _packageItemHead = d.ReadObject<TItem?[]>();
        _transformFunctions = d.ReadObject<Dictionary<string, TFunction>>();
    }

    internal void PostDeserialization( IActivityMonitor monitor )
    {
        foreach( var s in _sources.Values )
        {
            if( s is TFunctionSource functions )
            {
                functions.PostDeserialization( monitor, this );
            }
        }
    }

    internal void Serialize( IBinarySerializer s )
    {
        s.WriteObject( _sources );
        s.WriteObject( _items );
        s.WriteObject( _packageItemHead );
        s.WriteObject( _transformFunctions );
    }

}
