using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Core;

public sealed class ResourceSpace
{
    readonly Dictionary<object, ResPackage> _packageIndex;
    readonly ImmutableArray<ResourceSpaceHandler> _handlers;
    internal ImmutableArray<ResPackage> _packages;
    
    public ResourceSpace( Dictionary<object, ResPackage> packageIndex, ImmutableArray<ResourceSpaceHandler> handlers )
    {
        _packageIndex = packageIndex;
        _handlers = handlers;
    }

    public ImmutableArray<ResourceSpaceHandler> Handlers => _handlers;

    public bool Initialize( IActivityMonitor monitor )
    {
        bool success = true;
        foreach( var p in _packages )
        {
            foreach( var h in _handlers )
            {
                success &= h.
            }
        }
    }
}
