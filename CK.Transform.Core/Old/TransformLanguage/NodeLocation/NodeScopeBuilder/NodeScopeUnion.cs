using CK.Core;
using System;
using System.Diagnostics;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.Transform.Core;

/// <summary>
/// Builds union ranges.
/// </summary>
public sealed class NodeScopeUnion : NodeScopeBuilder
{
    readonly NodeScopeBuilder _left;
    readonly NodeScopeBuilder _right;
    readonly RangeUnioner _unioner;

    sealed class RangeUnioner
    {
        readonly BiRangeState _state;
        NodeLocationRange? _current;

        public RangeUnioner()
        {
            _state = new BiRangeState( true );
            _current = null;
        }

        public void Reset()
        {
            _state.Reset();
            _current = null;
        }

        public INodeLocationRange DoUnion( INodeLocationRange left, INodeLocationRange right, bool conclude )
        {
            _state.AddInputRanges( left, right );
            if( _current != null )
            {
                _state.ForwardLeftUntil( _current.End.Position );
                _state.ForwardRightUntil( _current.End.Position );
            }
            while( _state.BothHaveMore )
            {
                NodeLocationRange.Unified( _state.LeftE.Current, _state.RightE.Current, ProcessCurrent );
            }
            Debug.Assert( _state.LeftE.IsEmpty || _state.RightE.IsEmpty );
            if( conclude )
            {
                if( _state.LeftE.HasMore ) _state.FlushLeft( ref _current );
                else if( _state.RightE.HasMore ) _state.FlushLeft( ref _current, swapped: true );
                else
                {
                    if( _current != null ) _state.AddResult( _current );
                    _current = null;
                }
                Debug.Assert( _current == null );
            }
            return _state.ExtractResult();
        }

        INodeLocationRange ProcessCurrent( NodeLocationRange.Kind k, NodeLocationRange r1, NodeLocationRange r2 )
        {
            switch( k & ~NodeLocationRange.Kind.Swapped )
            {
                case NodeLocationRange.Kind.Equal:
                {
                    _state.MoveBoth();
                    return HandleUnioned( r1.MostPrecise( r2 ) );
                }
                case NodeLocationRange.Kind.Independent:
                {
                    _state.MoveLeft( (k & NodeLocationRange.Kind.Swapped) != 0 );
                    return HandleUnioned( r1 );
                }
                case NodeLocationRange.Kind.Contained:
                {
                    _state.MoveLeftOnceAndRightUntil( r1.End.Position, (k & NodeLocationRange.Kind.Swapped) != 0 );
                    return HandleUnioned( r1 );
                }
                case NodeLocationRange.Kind.SameStart:
                {
                    // Inverts left and right here.
                    _state.MoveLeftOnceAndRightUntil( r2.End.Position, (k & NodeLocationRange.Kind.Swapped) == 0 );
                    return HandleUnioned( new NodeLocationRange( r1.Beg.MostPrecise( r2.Beg ), r2.End ) );
                }
                case NodeLocationRange.Kind.SameEnd:
                {
                    _state.MoveBoth();
                    return HandleUnioned( new NodeLocationRange( r1.Beg, r1.End.MostPrecise( r2.End ) ) );
                }
                case NodeLocationRange.Kind.Overlapped:
                case NodeLocationRange.Kind.Congruent:
                {
                    // Inverts left and right here.
                    _state.MoveLeftOnceAndRightUntil( r2.End.Position, (k & NodeLocationRange.Kind.Swapped) == 0 );
                    return HandleUnioned( new NodeLocationRange( r1.Beg, r2.End ) );
                }
            }
            throw new NotImplementedException();
        }

        INodeLocationRange HandleUnioned( NodeLocationRange r )
        {
            Debug.Assert( r != null );
            if( _current == null ) _current = r;
            else
            {
                if( _current.End.Position >= r.Beg.Position )
                {
                    _current = _current.InternalSetEnd( r.End );
                }
                else
                {
                    _state.AddResult( _current );
                    _current = r;
                }
            }
            Debug.Assert( _current != null );
            return _current;
        }

    }

    public NodeScopeUnion( NodeScopeBuilder left, NodeScopeBuilder right )
    {
        Throw.CheckNotNullArgument( left );
        Throw.CheckNotNullArgument( right );
        _left = left.GetSafeBuilder();
        _right = right.GetSafeBuilder();
        _unioner = new RangeUnioner();
    }

    private protected override NodeScopeBuilder Clone() => new NodeScopeUnion( _left, _right );

    private protected override void DoReset()
    {
        _left.Reset();
        _right.Reset();
        _unioner.Reset();
    }

    private protected override INodeLocationRange DoEnter( IVisitContext context )
    {
        return Handle( _left.Enter( context ), _right.Enter( context ) );
    }

    private protected override INodeLocationRange DoLeave( IVisitContext context )
    {
        return Handle( _left.Leave( context ), _right.Leave( context ) );
    }

    private protected override INodeLocationRange DoConclude( IVisitContextBase context )
    {
        return Handle( _left.Conclude( context ), _right.Conclude( context ), true );
    }

    INodeLocationRange Handle( INodeLocationRange left, INodeLocationRange right, bool conclude = false )
    {
        if( left != null || right != null || conclude )
        {
            return _unioner.DoUnion( left, right, conclude );
        }
        return null;
    }

    internal static INodeLocationRange DoUnion( INodeLocationRange left, INodeLocationRange right )
    {
        return new RangeUnioner().DoUnion( left, right, true ) ?? NodeLocationRange.EmptySet;
    }

    /// <summary>
    /// Overridden to return a description of this builder.
    /// </summary>
    /// <returns>The union description.</returns>
    public override string ToString() => $"({_left} union {_right})";

}

