using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Checks the number of ranges and/or selects among them.
/// </summary>
public sealed class NodeScopeCardinalityFilter : NodeScopeBuilder
{
    readonly EachSplitter _inner;
    readonly LocationCardinalityInfo _info;
    readonly FIFOBuffer<NodeLocationRange> _lastBuffer;
    int _matchCount;
    int _eachNumber;
    bool _hasError;

    sealed class NodeLocationRangeSlice : INodeLocationRange
    {
        readonly INodeLocationRange _r;
        readonly int _offset;
        readonly int _count;

        NodeLocationRangeSlice( INodeLocationRange r, int offset, int count )
        {
            _r = r;
            _offset = offset;
            _count = count;
        }

        public static INodeLocationRange Create( INodeLocationRange r, int offset, int count )
        {
            if( r == null || count == 0 ) return NodeLocationRange.EmptySet;
            if( offset + count > r.Count || offset < 0 ) throw new ArgumentOutOfRangeException();
            if( count == r.Count ) return r;
            if( count == 1 )
            {
                if( offset == 0 ) return r.First;
                else if( offset == r.Count - 1 ) return r.Last;
                return r.ElementAt( offset );
            }
            return new NodeLocationRangeSlice( r, offset, count );
        }

        public int Count => _count;

        public NodeLocationRange First => _r.ElementAt( _offset );

        public NodeLocationRange Last => _r.ElementAt( _offset + _count );

        public IEnumerator<NodeLocationRange> GetEnumerator() => _r.Skip( _offset ).Take( _count ).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    sealed class EachSplitter
    {
        public readonly NodeScopeBuilder Inner;
        int _currentEachNumber;

        public EachSplitter( NodeScopeBuilder inner )
        {
            Inner = inner;
        }

        public void Reset()
        {
            Inner.Reset();
            _currentEachNumber = 0;
        }

        public IEnumerable<INodeLocationRange> Enter( IVisitContext context )
        {
            return Handle( Inner.Enter( context ), context );
        }

        public IEnumerable<INodeLocationRange> Leave( IVisitContext context )
        {
            return Handle( Inner.Leave( context ), context );
        }

        public IEnumerable<INodeLocationRange> Conclude( IVisitContextBase context )
        {
            var r = Handle( Inner.Conclude( context ), context );
            _currentEachNumber = 0;
            return r;
        }

        IEnumerable<INodeLocationRange>? Handle( INodeLocationRange inner, IVisitContextBase context )
        {
            if( inner == null || inner.Count <= 1 ) return inner;
            List<INodeLocationRange>? result = null;
            int idxLast = 0;
            int idx = 0;
            foreach( var r in inner )
            {
                if( r.EachNumber != _currentEachNumber )
                {
                    int countToEmit = idx - idxLast;
                    if( countToEmit > 0 )
                    {
                        result ??= new List<INodeLocationRange>();
                        result.Add( NodeLocationRangeSlice.Create( inner, idxLast, countToEmit ) );
                        idxLast = idx;
                    }
                    _currentEachNumber = r.EachNumber;
                }
                idx++;
            }
            if( result != null )
            {
                int remainder = idx - idxLast;
                if( remainder > 0 )
                {
                    result.Add( NodeLocationRangeSlice.Create( inner, idxLast, remainder ) );
                }
                return result;
            }
            return inner;
        }
    }

    public NodeScopeCardinalityFilter( NodeScopeBuilder inner, in LocationCardinalityInfo info )
    {
        Throw.CheckNotNullArgument( inner );
        _inner = new EachSplitter( inner.GetSafeBuilder() );
        _info = info;
        _eachNumber = -1;
        if( !_info.FromFirst ) _lastBuffer = new FIFOBuffer<NodeLocationRange>( _info.Offset + 1 );
    }

    private protected override NodeScopeBuilder Clone() => new NodeScopeCardinalityFilter( _inner.Inner, _info );

    private protected override void DoReset()
    {
        _inner.Reset();
        _eachNumber = -1;
        LocalStateReset();
    }

    private void LocalStateReset()
    {
        if( _lastBuffer != null ) _lastBuffer.Clear();
        _matchCount = 0;
        _hasError = false;
    }

    private protected override INodeLocationRange? DoEnter( IVisitContext context )
    {
        return Handle( _inner.Enter( context ), context );
    }

    private protected override INodeLocationRange? DoLeave( IVisitContext context )
    {
        return Handle( _inner.Leave( context ), context );
    }

    private protected override INodeLocationRange? DoConclude( IVisitContextBase context )
    {
        var r = Handle( _inner.Conclude( context ), context );
        if( !_hasError ) return LocalConclude( r, context );
        return null;
    }

    INodeLocationRange? LocalConclude( INodeLocationRange? r, IVisitContextBase context )
    {
        Debug.Assert( !_hasError );
        if( _matchCount < _info.ExpectedMatchCount )
        {
            context.Monitor.Error( $"Expected {_info.ExpectedMatchCount} ranges but found {_matchCount}." );
            _hasError = true;
        }
        if( _lastBuffer != null )
        {
            Debug.Assert( !_info.Each );
            int idx = _lastBuffer.Count - _info.Offset - 1;
            if( idx >= 0 ) r = _lastBuffer[idx];
        }
        return r;
    }

    INodeLocationRange? Handle( IEnumerable<INodeLocationRange> inner, IVisitContextBase context )
    {
        if( inner == null ) return null;
        var multi = inner as List<INodeLocationRange>;
        if( multi == null )
        {
            return HandleSameEachNumber( (INodeLocationRange)inner, context );
        }

        INodeLocationRange? result = null;
        foreach( var r in multi )
        {
            var one = HandleSameEachNumber( r, context );
            if( one != null )
            {
                result = result != null ? new LocationRangeCombined( result, one ) : one;
            }
        }
        return result;
    }

    INodeLocationRange HandleSameEachNumber( INodeLocationRange mono, IVisitContextBase context )
    {
        bool starting = _eachNumber == -1;
        if( starting || mono.First.EachNumber == _eachNumber )
        {
            _eachNumber = mono.First.EachNumber;
            return HandleWithCurrentState( mono, context );
        }
        INodeLocationRange? fromPrevious = null;
        if( !starting )
        {
            fromPrevious = LocalConclude( null, context );
            LocalStateReset();
        }
        _eachNumber = mono.First.EachNumber;
        var current = HandleWithCurrentState( mono, context );
        return fromPrevious != null
                ? (current != null ? new LocationRangeCombined( fromPrevious, current ) : fromPrevious)
                : current;
    }

    INodeLocationRange? HandleWithCurrentState( INodeLocationRange inner, IVisitContextBase context )
    {
        if( inner != null
            && inner.Count > 0
            && !_hasError
            && ShouldHandleBasedOnMatchCount( context.Monitor, inner.Count ) )
        {
            if( _lastBuffer != null )
            {
                Debug.Assert( !_info.FromFirst );
                foreach( var r in inner ) _lastBuffer.Push( r );
            }
            else
            {
                Debug.Assert( _info.FromFirst );
                if( _info.All )
                {
                    return _info.Each
                            ? ((INodeLocationRangeInternal)inner).InternalSetEachNumber()
                            : inner;
                }
                // ShouldHandleBasedOnMatchCount has incremented  _matchCount by inner.Count.
                int offsetInInner = _info.Offset - _matchCount + inner.Count;
                if( offsetInInner >= 0 )
                {
                    return offsetInInner == 0 ? inner.First : inner.ElementAt( offsetInInner );
                }
            }
        }
        return null;
    }

    bool ShouldHandleBasedOnMatchCount( IActivityMonitor monitor, int innerCount )
    {
        if( (_matchCount = _matchCount + innerCount) > 1
            && _info.ExpectedMatchCount > 0
            && _matchCount > _info.ExpectedMatchCount )
        {
            monitor.Error( $"Too many matches found for '{_inner.Inner}' (max is {_info.ExpectedMatchCount})." );
            _hasError = true;
        }
        else if( _info.All || !_info.FromFirst || _matchCount >= _info.Offset + 1 )
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Overridden to return the description of the cardinality.
    /// </summary>
    /// <returns>The description.</returns>
    public override string ToString() => $"(cardinality '{_info}' on {_inner.Inner})";


}


