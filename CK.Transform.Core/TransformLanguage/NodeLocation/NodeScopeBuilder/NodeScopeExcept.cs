using CK.Core;
using System;
using System.Diagnostics;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.Transform.TransformLanguage;


/// <summary>
/// Builds intersected ranges.
/// </summary>
public sealed class NodeScopeExcept : NodeScopeBuilder
{
    readonly NodeScopeBuilder _left;
    readonly NodeScopeBuilder _right;
    readonly RangeExceptor _exceptor;

    class RangeExceptor
    {
        readonly BiRangeState _state;
        NodeLocationRange? _current;

        public RangeExceptor()
        {
            _state = new BiRangeState( true );
            _current = null;
        }

        public void Reset()
        {
            _state.Reset();
            _current = null;
        }

        public INodeLocationRange? DoExcept( INodeLocationRange left, INodeLocationRange right, bool conclude )
        {
            _state.AddInputRanges( left, right );
            if( _current == null )
            {
                if( !_state.LeftE.HasMore ) return null;
                _current = _state.LeftE.Current;
            }
            Debug.Assert( _current != null );
            while( _current != null && _state.RightE.HasMore )
            {
                NodeLocationRange.Unified( _current, _state.RightE.Current, ProcessCurrent );
            }
            Debug.Assert( _current == null || _state.RightE.IsEmpty );
            if( conclude )
            {
                _state.FlushLeft( ref _current );
            }
            return _state.ExtractResult();
        }

        INodeLocationRange? ProcessCurrent( NodeLocationRange.Kind k, NodeLocationRange r1, NodeLocationRange r2 )
        {
            Debug.Assert( _current != null );
            Debug.Assert( (_current == r1 && (k & NodeLocationRange.Kind.Swapped) == 0) || (_current == r2 && (k & NodeLocationRange.Kind.Swapped) != 0) );
            switch( k )
            {
                case NodeLocationRange.Kind.Equal:
                case NodeLocationRange.Kind.Contained | NodeLocationRange.Kind.Swapped:
                case NodeLocationRange.Kind.SameEnd | NodeLocationRange.Kind.Swapped:
                {
                    _state.RightE.MoveNext();
                    _current = _state.LeftE.MoveNext() ? _state.LeftE.Current : null;
                    return null;
                }
                case NodeLocationRange.Kind.Congruent:
                case NodeLocationRange.Kind.Independent:
                {
                    _state.AddResult( _current );
                    _state.ForwardLeftUntil( r2.Beg.Position );
                    _current = _state.LeftE.HasMore ? _state.LeftE.Current : null;
                    return null;
                }
                case NodeLocationRange.Kind.Congruent | NodeLocationRange.Kind.Swapped:
                case NodeLocationRange.Kind.Independent | NodeLocationRange.Kind.Swapped:
                {
                    _state.ForwardRightUntil( r2.Beg.Position );
                    return null;
                }
                case NodeLocationRange.Kind.Contained:
                {
                    _state.AddResult( new NodeLocationRange( r1.Beg, r2.Beg ) );
                    _current = _current.InternalSetBeg( r2.End );
                    _state.RightE.MoveNext();
                    return null;
                }

                case NodeLocationRange.Kind.SameStart:
                {
                    _state.ForwardLeftUntil( r2.End.Position );
                    _current = _state.LeftE.HasMore ? _state.LeftE.Current : null;
                    return null;
                }
                case NodeLocationRange.Kind.SameStart | NodeLocationRange.Kind.Swapped:
                {
                    _current = _current.InternalSetBeg( r1.End );
                    _state.ForwardRightUntil( r2.End.Position );
                    return null;
                }
                case NodeLocationRange.Kind.Overlapped:
                case NodeLocationRange.Kind.SameEnd:
                {
                    _current = _current.InternalSetEnd( r2.Beg );
                    _state.AddResult( _current );
                    _state.ForwardLeftUntil( r2.End.Position );
                    _current = _state.LeftE.HasMore ? _state.LeftE.Current : null;
                    return null;
                }
                case NodeLocationRange.Kind.Overlapped | NodeLocationRange.Kind.Swapped:
                {
                    _current = _current.InternalSetBeg( r1.End );
                    _state.RightE.MoveNext();
                    return null;
                }
            }
            throw new NotImplementedException();
        }
    }

    public NodeScopeExcept( NodeScopeBuilder left, NodeScopeBuilder right )
    {
        Throw.CheckNotNullArgument( left );
        Throw.CheckNotNullArgument( right );
        _left = left.GetSafeBuilder();
        _right = right.GetSafeBuilder();
        _exceptor = new RangeExceptor();
    }

    private protected override void DoReset()
    {
        _left.Reset();
        _right.Reset();
        _exceptor.Reset();
    }

    private protected override NodeScopeBuilder Clone() => new NodeScopeExcept( _left, _right );

    private protected override INodeLocationRange? DoEnter( IVisitContext context )
    {
        return Handle( _left.Enter( context ), _right.Enter( context ) );
    }

    private protected override INodeLocationRange? DoLeave( IVisitContext context )
    {
        return Handle( _left.Leave( context ), _right.Leave( context ) );
    }

    private protected override INodeLocationRange? DoConclude( IVisitContextBase context )
    {
        return Handle( _left.Conclude( context ), _right.Conclude( context ), true );
    }

    INodeLocationRange Handle( INodeLocationRange left, INodeLocationRange right, bool conclude = false )
    {
        if( left != null || right != null || conclude )
        {
            return _exceptor.DoExcept( left, right, conclude );
        }
        return null;
    }

    internal static INodeLocationRange DoExcept( INodeLocationRange left, INodeLocationRange right )
    {
        return new RangeExceptor().DoExcept( left, right, true ) ?? NodeLocationRange.EmptySet;
    }

    /// <summary>
    /// Overridden to return a description of this builder.
    /// </summary>
    /// <returns>The except description.</returns>
    public override string ToString() => $"({_left} except {_right})";

}


