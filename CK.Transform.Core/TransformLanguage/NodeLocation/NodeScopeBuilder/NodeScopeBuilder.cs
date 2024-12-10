using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Abstract range builder.
/// </summary>
public abstract class NodeScopeBuilder
{
    INodeLocationRangeInternal? _last;
    readonly bool _autoMergeContiguous;
    bool _inUse;

    /// <summary>
    /// Initializes a new <see cref="NodeScopeBuilder"/> that, by default, merges emitted 
    /// directly subsequent ranges (ie. [1,8[ and [8,12[ are merged as [1,12[). 
    /// </summary>
    /// <param name="autoMergeContiguous">True to emit merged contiguous ranges.</param>
    protected NodeScopeBuilder( bool autoMergeContiguous = false )
    {
        _autoMergeContiguous = autoMergeContiguous;
    }

    /// <summary>
    /// Resets any internal state
    /// </summary>
    public void Reset()
    {
        _last = null;
        DoReset();
    }

    [DebuggerStepThrough]
    internal INodeLocationRange? Enter( IVisitContext context )
    {
        return Handle( (INodeLocationRangeInternal?)DoEnter( context ) );
    }

    [DebuggerStepThrough]
    internal INodeLocationRange? Leave( IVisitContext context )
    {
        return Handle( (INodeLocationRangeInternal?)DoLeave( context ) );
    }

    internal INodeLocationRange? Conclude( IVisitContextBase context )
    {
        var r1 = (INodeLocationRangeInternal?)Handle( (INodeLocationRangeInternal?)DoConclude( context ) );
        var r2 = _last;
        if( r2 != null )
        {
            _last = null;
            return r1 != null ? new LocationRangeCombined( r1, r2 ) : r2;
        }
        return r1;
    }

    internal NodeScopeBuilder GetSafeBuilder()
    {
        if( !_inUse )
        {
            _inUse = true;
            return this;
        }
        return Clone();
    }

    /// <summary>
    /// Must reset any internal state.
    /// </summary>
    private protected abstract void DoReset();

    /// <summary>
    /// Must provide a clone of this builder.
    /// </summary>
    private protected abstract NodeScopeBuilder Clone();

    /// <summary>
    /// Called for each node, before visiting its children. May return a range.
    /// </summary>
    /// <param name="context">The visited node and location manager to use.</param>
    /// <returns>Null or a range to consider.</returns>
    private protected abstract INodeLocationRange? DoEnter( IVisitContext context );

    /// <summary>
    /// Called for each node, before visiting its children. May return a range.
    /// </summary>
    /// <param name="context">The visited node and location manager to use.</param>
    /// <returns>Null or a range to consider.</returns>
    private protected abstract INodeLocationRange? DoLeave( IVisitContext context );

    /// <summary>
    /// Called at the end of the visit.
    /// </summary>
    /// <param name="context">Base context (offers location manager and error management).</param>
    /// <returns>Null or the final range to consider.</returns>
    private protected abstract INodeLocationRange? DoConclude( IVisitContextBase context );

    INodeLocationRange? Handle( INodeLocationRangeInternal? r )
    {
        if( r == null || r == NodeLocationRange.EmptySet ) return null;

        INodeLocationRangeInternal? result = _last;
        if( result != null )
        {
            var l = result.Last;
            Throw.CheckState( "Newly built range intersects previous one.", l.End.Position <= r.First.Beg.Position );

            if( _autoMergeContiguous && l.End.Position == r.First.Beg.Position )
            {
                _last = result.InternalSetEnd( r.Last.End );
                return null;
            }
        }
        _last = r;
        return result;
    }

    /// <summary>
    /// Reusable range enumerator over multiple ranges.
    /// </summary>
    private protected class RangeEnumerator : IEnumerator<NodeLocationRange>
    {
        IEnumerator<NodeLocationRange>? _current;
        IEnumerable<NodeLocationRange>? _next;

        /// <summary>
        /// Initializes a new RangeEnumerator.
        /// </summary>
        public RangeEnumerator()
        {
        }

        RangeEnumerator( IEnumerator<NodeLocationRange> current, IEnumerable<NodeLocationRange> next )
        {
            _current = current;
            _next = next;
        }

        /// <summary>
        /// Appends an enumerable. Either this RangeEnumerator or a new one is returned.
        /// </summary>
        /// <param name="next">The next enumerable. Can be null.</param>
        /// <returns>This or a new one that combines this and the next range.</returns>
        public RangeEnumerator Add( IEnumerable<NodeLocationRange>? next )
        {
            if( next == null ) return this;
            if( _current == null )
            {
                Debug.Assert( _next == null );
                _current = next.GetEnumerator();
                if( !_current.MoveNext() )
                {
                    _current = null;
                }
                return this;
            }
            if( _next == null )
            {
                _next = next;
                return this;
            }
            return new RangeEnumerator( this, next );
        }

        /// <summary>
        /// Gets whether this RangeEnumerator has no more <see cref="Current"/> range.
        /// </summary>
        public bool IsEmpty => _current == null;

        /// <summary>
        /// Gets whether this RangeEnumerator has a <see cref="Current"/> range.
        /// </summary>
        public bool HasMore => _current != null;

        /// <summary>
        /// Gets the current range.
        /// </summary>
        public NodeLocationRange Current
        {
            get
            {
                Throw.CheckState( _current != null );
                return _current.Current;
            }
        }

        /// <summary>
        /// Moves to the next range if possible.
        /// </summary>
        /// <returns>True if the move succeeded (<see cref="IsEmpty"/> is false).</returns>
        public bool MoveNext()
        {
            if( _current == null || !_current.MoveNext() )
            {
                if( _next == null )
                {
                    _current = null;
                    return false;
                }
                _current = _next.GetEnumerator();
                _next = null;
                if( !_current.MoveNext() )
                {
                    _current = null;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Resets the current ranges.
        /// </summary>
        public void Reset()
        {
            _current = null;
            _next = null;
        }

        object IEnumerator.Current
        {
            get
            {
                Throw.CheckState( _current != null );
                return _current.Current;
            }
        }

        void IDisposable.Dispose()
        {
        }

    }

    private protected readonly struct RangeBuffer
    {
        readonly List<NodeLocationRange> _buffer;

        public RangeBuffer()
        {
            _buffer = new List<NodeLocationRange>();
        }

        public void Reset() => _buffer.Clear();

        public void AddResult( NodeLocationRange r ) => _buffer.Add( r );

        public INodeLocationRange ExtractResult()
        {
            INodeLocationRange r = NodeLocationRange.EmptySet;
            if( _buffer.Count > 0 )
            {
                r = NodeLocationRange.Create( _buffer, _buffer.Count, true );
                _buffer.Clear();
            }
            return r;
        }
    }

    /// <summary>
    /// Helper class that factorizes code between union and except implementations.
    /// </summary>
    private protected struct BiRangeState
    {
        readonly RangeBuffer _buffer;
        RangeEnumerator _leftE;
        RangeEnumerator _rightE;

        public RangeEnumerator LeftE => _leftE;
        public RangeEnumerator RightE => _rightE;

        public BiRangeState( bool onlyCtor )
        {
            _leftE = new RangeEnumerator();
            _rightE = new RangeEnumerator();
            _buffer = new RangeBuffer();
        }

        public void Reset()
        {
            _leftE.Reset();
            _rightE.Reset();
            _buffer.Reset();
        }

        public void AddInputRanges( INodeLocationRange left, INodeLocationRange right )
        {
            _leftE = _leftE.Add( left );
            _rightE = _rightE.Add( right );
        }

        public bool BothHaveMore => _leftE.HasMore && _rightE.HasMore;

        public void ForwardLeftUntil( int position )
        {
            if( _leftE.HasMore ) while( _leftE.Current.End.Position <= position && _leftE.MoveNext() ) ;
        }

        public void ForwardRightUntil( int position )
        {
            if( _rightE.HasMore ) while( _rightE.Current.End.Position <= position && _rightE.MoveNext() ) ;
        }

        public void MoveLeftOnceAndRightUntil( int position, bool swapped )
        {
            if( swapped )
            {
                _rightE.MoveNext();
                while( _leftE.MoveNext() && _leftE.Current.End.Position <= position ) ;
            }
            else
            {
                _leftE.MoveNext();
                while( _rightE.MoveNext() && _rightE.Current.End.Position <= position ) ;
            }
        }

        public void MoveLeft( bool swapped )
        {
            if( swapped )
            {
                _rightE.MoveNext();
            }
            else
            {
                _leftE.MoveNext();
            }
        }

        public void MoveBoth()
        {
            _leftE.MoveNext();
            _rightE.MoveNext();
        }

        public void AddResult( NodeLocationRange r ) => _buffer.AddResult( r );

        public INodeLocationRange ExtractResult() => _buffer.ExtractResult();

        /// <summary>
        /// Unconditionnaly flushes left ranges (or right if swapped is true) to the results, combining
        /// the remaining ranges with a current one that will be null after this.
        /// </summary>
        /// <param name="current">A current range that will be extended and emitted if not null.</param>
        /// <param name="swapped">True to flush right instead of left.</param>
        public void FlushLeft( ref NodeLocationRange? current, bool swapped = false )
        {
            RangeEnumerator e = swapped ? _rightE : _leftE;
            if( current != null )
            {
                while( current.End.Position >= e.Current.Beg.Position )
                {
                    if( e.Current.End.Position > current.End.Position ) current = current.InternalSetEnd( e.Current.End );
                    if( !e.MoveNext() ) break;
                }
                AddResult( current );
                current = null;
            }
            if( e.HasMore )
            {
                do
                {
                    AddResult( e.Current );
                }
                while( e.MoveNext() );
            }
        }
    }
}

