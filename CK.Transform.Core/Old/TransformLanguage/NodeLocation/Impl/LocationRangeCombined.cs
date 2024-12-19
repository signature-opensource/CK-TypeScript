using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.Transform.TransformLanguage;

internal sealed class LocationRangeCombined : INodeLocationRangeInternal
{
    readonly INodeLocationRangeInternal _r1;
    readonly INodeLocationRangeInternal _r2;

    internal LocationRangeCombined( INodeLocationRange r1, INodeLocationRange r2 )
    {
        Debug.Assert( r1 != null && r1 != NodeLocationRange.EmptySet );
        Debug.Assert( r2 != null && r2 != NodeLocationRange.EmptySet );
        _r1 = (INodeLocationRangeInternal)r1;
        _r2 = (INodeLocationRangeInternal)r2;
    }

    public int Count => _r1.Count + _r2.Count;

    public NodeLocationRange First => _r1.First;

    public NodeLocationRange Last => _r2.Last;

    public IEnumerator<NodeLocationRange> GetEnumerator() => _r1.Concat( _r2 ).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public INodeLocationRangeInternal InternalSetEnd( NodeLocation end )
    {
        return new LocationRangeCombined( _r1, _r2.InternalSetEnd( end ) );
    }

    public INodeLocationRangeInternal InternalSetEachNumber( int value )
    {
        return new LocationRangeCombined( _r1.InternalSetEachNumber( value ), _r2.InternalSetEachNumber( value ) );
    }

    public override string ToString()
    {
        return string.Join( "-", this.Select( r => r.ToString() ) );
    }


}
