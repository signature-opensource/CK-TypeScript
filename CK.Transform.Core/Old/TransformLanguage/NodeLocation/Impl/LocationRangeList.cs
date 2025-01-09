using CK.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CK.Transform.Core;

internal class LocationRangeList : INodeLocationRange, INodeLocationRangeInternal
{
    readonly IReadOnlyList<NodeLocationRange> _v;

    internal LocationRangeList( params NodeLocationRange[] values )
        : this( (IReadOnlyList<NodeLocationRange>)values )
    {
    }

    internal LocationRangeList( IReadOnlyList<NodeLocationRange> list )
    {
        Throw.DebugAssert( list != null && list.Count > 1 && list.All( r => r != null && r != NodeLocationRange.EmptySet ) );
        _v = list;
    }

    public int Count => _v.Count;

    public NodeLocationRange First => _v[0];

    public NodeLocationRange Last => _v[_v.Count - 1];

    public IEnumerator<NodeLocationRange> GetEnumerator() => _v.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _v.GetEnumerator();

    public INodeLocationRangeInternal InternalSetEnd( NodeLocation end )
    {
        var v = _v.ToArray();
        v[v.Length - 1] = v[v.Length - 1].InternalSetEnd( end );
        return new LocationRangeList( v );
    }

    public INodeLocationRangeInternal InternalSetEachNumber( int value )
    {
        return new LocationRangeList( _v.Select( r => r.InternalSetEachNumber( value ) ).ToArray() );
    }

    public override string ToString()
    {
        return string.Join( "-", this.Select( r => r.ToString() ) );
    }

}
