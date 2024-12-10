using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Builds mono-node ranges for nodes that have a matching trivia.
/// </summary>
public sealed class NodeScopeFromTriviaMatcher : NodeScopeBuilder
{
    readonly Func<Trivia, bool> _triviaMatcher;
    readonly string _triviaDescription;
    readonly bool _nodeAfter;
    readonly List<int> _posAhead;
    int _prev0;
    int _prev1;

    /// <summary>
    /// Initializes a new <see cref="NodeScopeFromTriviaMatcher"/>.
    /// </summary>
    /// <param name="nodeAfter">Whether the node after the match must be selected (or the node before).</param>
    /// <param name="triviaMatcher">The trivia predicate.</param>
    /// <param name="triviaDescription">The description of the trivia predicate.</param>
    public NodeScopeFromTriviaMatcher( bool nodeAfter, Func<Trivia, bool> triviaMatcher, string triviaDescription )
    {
        Throw.CheckNotNullArgument( triviaMatcher );
        Throw.CheckNotNullArgument( triviaDescription );
        _triviaMatcher = triviaMatcher;
        _triviaDescription = triviaDescription;
        _nodeAfter = nodeAfter;
        _posAhead = new List<int>();
        _prev0 = _prev1 = -2;
    }

    private protected override NodeScopeBuilder Clone() => new NodeScopeFromTriviaMatcher( _nodeAfter, _triviaMatcher, _triviaDescription );

    private protected override void DoReset()
    {
        _posAhead.Clear();
        _prev0 = _prev1 = -2;
    }

    private protected override INodeLocationRange? DoEnter( IVisitContext context )
    {
        int pos = context.Position;
        Debug.Assert( _posAhead.Count == 0 || _posAhead[0] >= pos - 1 );
        bool emitBefore = false;
        bool emitCurrent = false;
        if( _posAhead.Count > 0 && _posAhead[0] <= pos )
        {
            if( _posAhead[0] == pos - 1 )
            {
                emitBefore = true;
                _posAhead.RemoveAt( 0 );
            }
            if( _posAhead.Count > 0 && _posAhead[0] == pos )
            {
                emitCurrent = true;
                _posAhead.RemoveAt( 0 );
            }
        }
        if( pos - 1 == _prev1 && pos == _prev0 ) return null;

        var n = context.VisitedNode;
        if( n.LeadingTrivias.Any( _triviaMatcher ) )
        {
            if( _nodeAfter ) emitCurrent = true;
            else emitBefore = true;
        }
        if( n.TrailingTrivias.Any( _triviaMatcher ) )
        {
            if( _nodeAfter ) AddAhead( context.Position + n.Width );
            else emitCurrent = true;
        }
        emitBefore &= pos - 1 != _prev0 && pos - 1 != _prev1;
        emitCurrent &= pos != _prev0 && pos != _prev1;

        if( emitBefore )
        {
            _prev1 = pos - 1;
            var current = context.GetCurrentLocation();
            var before = current.Predecessor();
            if( before.IsBegMarker ) before = current;
            var beforeRange = new NodeLocationRange( before, current );
            if( emitCurrent )
            {
                _prev0 = pos;
                return new LocationRangeCombined( beforeRange, new NodeLocationRange( current, current.Successor() ) );
            }
            return beforeRange;
        }
        if( emitCurrent )
        {
            _prev0 = pos;
            var current = context.GetCurrentLocation();
            return new NodeLocationRange( current, current.Successor() );
        }
        return null;
    }

    void AddAhead( int position )
    {
        int idx = _posAhead.BinarySearch( position );
        if( idx < 0 ) _posAhead.Insert( ~idx, position );
    }

    private protected override INodeLocationRange? DoLeave( IVisitContext context ) => null;

    private protected override INodeLocationRange? DoConclude( IVisitContextBase context )
    {
        Debug.Assert( _posAhead.Count == 0 || _posAhead[0] == context.LocationManager.EndMarker.Position );
        return null;
    }


    /// <summary>
    /// Overridden to return the description of this builder.
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString() => _nodeAfter ? $"(after {_triviaDescription})" : $"(before {_triviaDescription})";

}


