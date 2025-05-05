using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

sealed class SingleSpanTypeFilter : IFilteredTokenEnumerableProvider
{
    readonly Type _spanType;

    public SingleSpanTypeFilter( Type spanType )
    {
        _spanType = spanType;
    }

    public Func<IActivityMonitor,
                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection()
    {
        throw new NotImplementedException();
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Run( IActivityMonitor monitor, IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        foreach( var each in inner )
            foreach( var range in inner )

    }
}
