using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.Core;

readonly struct XNode<T1, T2, T3, T4, T5>
    where T1 : AbstractNode
    where T2 : AbstractNode
    where T3 : AbstractNode
    where T4 : AbstractNode
    where T5 : AbstractNode
{
    public readonly T1? V1;
    public readonly T2? V2;
    public readonly T3? V3;
    public readonly T4? V4;
    public readonly T5? V5;
    readonly AbstractNode[] _content;


    public XNode( T1? o1, T2? o2, T3? o3, T4? o4, T5? o5 )
    {
        int count = (V1 = o1) != null ? 1 : 0;
        if( (V2 = o2) != null ) ++count;
        if( (V3 = o3) != null ) ++count;
        if( (V4 = o4) != null ) ++count;
        if( (V5 = o5) != null ) ++count;
        _content = new AbstractNode[count];
        count = 0;
        if( o1 != null ) _content[++count] = o1;
        if( o2 != null ) _content[++count] = o2;
        if( o3 != null ) _content[++count] = o3;
        if( o4 != null ) _content[++count] = o4;
        if( o5 != null ) _content[++count] = o5;
    }

    public XNode( IEnumerable<AbstractNode> a )
    {
        using( var x = a.GetEnumerator() )
        {
            if( x.MoveNext() )
            {
                Count = (V1 = (T1)x.Current) != null ? 1 : 0;
                if( x.MoveNext() )
                {
                    if( (V2 = (T2)x.Current) != null ) ++Count;
                    if( x.MoveNext() )
                    {
                        if( (V3 = (T3)x.Current) != null ) ++Count;
                        if( x.MoveNext() )
                        {
                            if( (V4 = (T4)x.Current) != null ) ++Count;
                            if( x.MoveNext() )
                            {
                                if( (V5 = (T5)x.Current) != null ) ++Count;
                                if( !x.MoveNext() ) return;
                            }
                        }
                    }
                }
            }
        }
    }

    public IReadOnlyList<AbstractNode> Content => _content;

    public IList<AbstractNode> GetRawContent() => new AbstractNode?[] { V1, V2, V3, V4, V5 }!;
}


readonly struct SNode<T1, T2, T3, T4, T5> : IReadOnlyList<AbstractNode>
    where T1 : AbstractNode
    where T2 : AbstractNode
    where T3 : AbstractNode
    where T4 : AbstractNode
    where T5 : AbstractNode
{
    public readonly T1 V1;
    public readonly T2 V2;
    public readonly T3 V3;
    public readonly T4 V4;
    public readonly T5 V5;
    public readonly int Count;


    public SNode( T1 o1, T2 o2, T3 o3, T4 o4, T5 o5 )
    {
        Count = (V1 = o1) != null ? 1 : 0;
        if( (V2 = o2) != null ) ++Count;
        if( (V3 = o3) != null ) ++Count;
        if( (V4 = o4) != null ) ++Count;
        if( (V5 = o5) != null ) ++Count;
    }

    public SNode( IEnumerable<AbstractNode> a )
    {
        using( var x = a.GetEnumerator() )
        {
            if( x.MoveNext() )
            {
                Count = (V1 = (T1)x.Current) != null ? 1 : 0;
                if( x.MoveNext() )
                {
                    if( (V2 = (T2)x.Current) != null ) ++Count;
                    if( x.MoveNext() )
                    {
                        if( (V3 = (T3)x.Current) != null ) ++Count;
                        if( x.MoveNext() )
                        {
                            if( (V4 = (T4)x.Current) != null ) ++Count;
                            if( x.MoveNext() )
                            {
                                if( (V5 = (T5)x.Current) != null ) ++Count;
                                if( !x.MoveNext() ) return;
                            }
                        }
                    }
                }
            }
        }
    }

    public AbstractNode this[int index]
    {
        get
        {
            if( index < 0 || index >= Count ) throw new IndexOutOfRangeException();
            switch( index )
            {
                case 0: return (AbstractNode?)V1 ?? (AbstractNode?)V2 ?? (AbstractNode?)V3 ?? (AbstractNode?)V4 ?? V5!;
                case 1: return (AbstractNode?)V2 ?? (AbstractNode?)V3 ?? (AbstractNode?)V4 ?? V5!;
                case 2: return (AbstractNode?)V3 ?? (AbstractNode?)V4 ?? V5!;
                case 3: return (AbstractNode?)V4 ?? V5!;
                default: return V5!;
            }
        }
    }

    int IReadOnlyCollection<AbstractNode>.Count => Count;

    public IEnumerator<AbstractNode> GetEnumerator()
    {
        if( V1 != null ) yield return V1;
        if( V2 != null ) yield return V2;
        if( V3 != null ) yield return V3;
        if( V4 != null ) yield return V4;
        if( V5 != null ) yield return V5;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IList<AbstractNode> GetRawContent() => new AbstractNode?[] { V1, V2, V3, V4, V5 }!;
}

