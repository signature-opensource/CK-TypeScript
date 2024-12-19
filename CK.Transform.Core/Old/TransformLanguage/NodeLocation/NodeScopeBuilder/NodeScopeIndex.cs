using CK.Core;
using System.Collections.Generic;
using System.Linq;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.Transform.TransformLanguage;


/// <summary>
/// Filters a subset of ranges based on an index and an a count.
/// </summary>
public sealed class NodeScopeIndex : NodeScopeBuilder
{
    readonly NodeScopeBuilder _inner;
    readonly int _start;
    readonly int _count;
    readonly int _stop;
    int _currentIdx;

    public NodeScopeIndex( NodeScopeBuilder inner, int start = 0, int count = -1 )
    {
        Throw.CheckNotNullArgument( inner );
        _inner = inner.GetSafeBuilder();
        _start = start;
        _count = count;
        _stop = count < 0 ? int.MaxValue : _start + count;
    }

    private protected override NodeScopeBuilder Clone() => new NodeScopeIndex( _inner, _start, _count );

    private protected override void DoReset()
    {
        _inner.Reset();
        _currentIdx = 0;
    }

    private protected override INodeLocationRange DoEnter( IVisitContext context )
    {
        return Handle( _inner.Enter( context ), context );
    }

    private protected override INodeLocationRange DoLeave( IVisitContext context )
    {
        return Handle( _inner.Leave( context ), context );
    }

    private protected override INodeLocationRange DoConclude( IVisitContextBase context )
    {
        return Handle( _inner.Conclude( context ), context );
    }

    INodeLocationRange Handle( INodeLocationRange inner, IVisitContextBase context )
    {
        if( inner == null ) return null;
        int nbInner = inner.Count;
        int futureIdx = _currentIdx + nbInner;
        if( futureIdx < _start || _currentIdx > _stop ) return null;

        IEnumerable<NodeLocationRange> e = inner;
        int deltaFront = _start - _currentIdx;
        if( deltaFront > 0 )
        {
            e = e.Skip( deltaFront );
            nbInner -= deltaFront;
            _currentIdx = _start;
        }
        if( nbInner > _count ) nbInner = _count;
        return NodeLocationRange.Create( e.Take( nbInner ), nbInner, true );
    }

}

