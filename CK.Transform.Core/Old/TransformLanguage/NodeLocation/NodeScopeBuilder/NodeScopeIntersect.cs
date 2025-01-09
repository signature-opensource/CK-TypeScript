using CK.Core;
using System.Diagnostics;

namespace CK.Transform.Core;


/// <summary>
/// Builds intersected ranges.
/// </summary>
public sealed class NodeScopeIntersect : NodeScopeBuilder
{
    readonly NodeScopeBuilder _left;
    readonly NodeScopeBuilder _right;
    readonly RangeIntersector _state;

    struct RangeIntersector
    {
        readonly RangeBuffer _buffer;
        RangeEnumerator _leftE;
        RangeEnumerator _rightE;

        public RangeIntersector( bool onlyCtor )
        {
            _buffer = new RangeBuffer();
            _leftE = new RangeEnumerator();
            _rightE = new RangeEnumerator();
        }

        public void Reset()
        {
            _leftE.Reset();
            _rightE.Reset();
            _buffer.Reset();
        }

        public INodeLocationRange DoIntersect( INodeLocationRange left, INodeLocationRange right )
        {
            Debug.Assert( left != null || right != null );
            _leftE = _leftE.Add( left );
            _rightE = _rightE.Add( right );
            while( _leftE.HasMore && _rightE.HasMore )
            {
                NodeLocationRange l = _leftE.Current.Intersect( _rightE.Current );
                if( l != NodeLocationRange.EmptySet ) _buffer.AddResult( l );
                bool forward1 = _leftE.Current.End.Position <= _rightE.Current.End.Position;
                bool forward2 = _leftE.Current.End.Position >= _rightE.Current.End.Position;
                if( forward1 ) _leftE.MoveNext();
                if( forward2 ) _rightE.MoveNext();
            }
            return _buffer.ExtractResult();
        }

    }

    public NodeScopeIntersect( NodeScopeBuilder left, NodeScopeBuilder right )
    {
        Throw.CheckNotNullArgument( left );
        Throw.CheckNotNullArgument( right );
        _left = left.GetSafeBuilder();
        _right = right.GetSafeBuilder();
        _state = new RangeIntersector( true );
    }

    private protected override NodeScopeBuilder Clone() => new NodeScopeIntersect( _left, _right );

    private protected override void DoReset()
    {
        _left.Reset();
        _right.Reset();
        _state.Reset();
    }

    private protected override INodeLocationRange? DoEnter( IVisitContext context )
    {
        return StateIntersect( _left.Enter( context ), _right.Enter( context ) );
    }

    private protected override INodeLocationRange? DoLeave( IVisitContext context )
    {
        return StateIntersect( _left.Leave( context ), _right.Leave( context ) );
    }

    private protected override INodeLocationRange? DoConclude( IVisitContextBase context )
    {
        return StateIntersect( _left.Conclude( context ), _right.Conclude( context ) );
    }

    INodeLocationRange? StateIntersect( INodeLocationRange left, INodeLocationRange right )
    {
        return left != null || right != null
                ? _state.DoIntersect( left, right )
                : null;
    }

    internal static INodeLocationRange DoIntersect( INodeLocationRange? left, INodeLocationRange? right )
    {
        return left == null || left == NodeLocationRange.EmptySet || right == null || right == NodeLocationRange.EmptySet
                ? NodeLocationRange.EmptySet
                : new RangeIntersector( true ).DoIntersect( left, right ) ?? NodeLocationRange.EmptySet;
    }

    /// <summary>
    /// Overridden to return a description of this builder.
    /// </summary>
    /// <returns>The intersect description.</returns>
    public override string ToString() => $"({_left} intersect {_right})";
}

