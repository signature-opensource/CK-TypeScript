using CK.Core;
using CK.Transform.Core;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace CK.Transform.TransformLanguage;

sealed class LocationInserter
{
    readonly LocationInfo _finderInfo;
    readonly FIFOBuffer<MatchedNode>? _lastBuffer;
    int _matchCount;
    bool _hasError;

    public sealed class MatchedNode
    {
        public readonly int Position;
        public readonly AbstractNode Node;
        public readonly IReadOnlyList<int>? IdxTrivias;

        public MatchedNode( int p, AbstractNode n, IReadOnlyList<int>? t )
        {
            Position = p;
            Node = n;
            IdxTrivias = t;
        }

        public AbstractNode Apply( IActivityMonitor monitor, string? before, string? after, bool clearBlockComments, Trivia[]? triviaReplacement )
        {
            Throw.DebugAssert( (before != null || after != null) && triviaReplacement == null
                               || (before == null && after == null) && triviaReplacement != null );
            var e = Node;
            if( clearBlockComments )
            {
                var cleaner = new TriviaCleaner( monitor, false, true, true );
                e = cleaner.VisitRoot( e );
            }
            int deltaInsert = 0;
            bool inTrailing = false;
            if( IdxTrivias == null )
            {
                Throw.DebugAssert( "When matching nodes, only before/after are handled.", triviaReplacement == null && (after != null || before != null) );
                ImmutableArray<Trivia> leading, trailing;
                if( clearBlockComments )
                {
                    e = e.LiftBothTrivias();
                    leading = before != null
                                ? e.LeadingTrivias.Add( new Trivia( TokenType.Whitespace, before ) )
                                : e.LeadingTrivias;
                    trailing = after != null
                                ? e.TrailingTrivias.Insert( 0, new Trivia( TokenType.Whitespace, after ) )
                                : e.TrailingTrivias;
                }
                else
                {
                    leading = before != null
                                ? e.LeadingTrivias.Insert( 0, new Trivia( TokenType.Whitespace, before ) )
                                : e.LeadingTrivias;
                    trailing = after != null
                                ? e.TrailingTrivias.Add( new Trivia( TokenType.Whitespace, after ) )
                                : e.TrailingTrivias;
                }
                return e.SetTrivias( leading, trailing );
            }
            foreach( int idx in IdxTrivias )
            {
                ImmutableArray<Trivia> trivias;
                int actualIdx;
                if( idx >= 0 )
                {
                    trivias = e.LeadingTrivias;
                    actualIdx = idx + deltaInsert;
                }
                else
                {
                    if( !inTrailing )
                    {
                        inTrailing = true;
                        deltaInsert = 0;
                    }
                    trivias = e.TrailingTrivias;
                    actualIdx = ~idx + deltaInsert;
                }
                if( triviaReplacement != null )
                {
                    Debug.Assert( before == null && after == null );
                    trivias = trivias.RemoveAt( actualIdx );
                    trivias = trivias.InsertRange( actualIdx, triviaReplacement );
                    // Replacing trivias is only for first autoclosed extension tag injection.
                    // Updating the deltaInsert is actually useless but this is clearer
                    // to keep this.
                    deltaInsert += 2;
                }
                else
                {
                    if( before != null )
                    {
                        trivias = trivias.Insert( actualIdx++, new Trivia( TokenType.Whitespace, before ) );
                        ++deltaInsert;
                    }
                    if( after != null )
                    {
                        trivias = trivias.Insert( actualIdx + 1, new Trivia( TokenType.Whitespace, after ) );
                        ++deltaInsert;
                    }
                }
                e = idx >= 0 ? e.SetTrivias( trivias, e.TrailingTrivias ) : e.SetTrivias( e.LeadingTrivias, trivias );
            }
            return e;
        }
    }

    public LocationInserter( in LocationInfo finderInfo )
    {
        _finderInfo = finderInfo;
        if( !_finderInfo.Card.FromFirst )
        {
            // When not matching from first, we need a buffer: Conclude will
            // provide the result.
            _lastBuffer = new FIFOBuffer<MatchedNode>( _finderInfo.Card.Offset + 1 );
        }
    }

    /// <summary>
    /// Gets the match count.
    /// </summary>
    public int MatchCount => _matchCount;

    /// <summary>
    /// Gets the expected total match count. 
    /// Zero when not applicable (when there is no 'out of n' specified).
    /// </summary>
    public int ExpectedMatchCount => _finderInfo.Card.ExpectedMatchCount;

    public bool CanStop => _hasError
                            || (_finderInfo.Card.FromFirst
                                && _finderInfo.Card.ExpectedMatchCount == 0
                                && !_finderInfo.Card.All
                                && _matchCount == _finderInfo.Card.Offset + 1);

    public bool RequiresConclude => !_finderInfo.Card.FromFirst && !_hasError;

    public MatchedNode? AddCandidate( IActivityMonitor monitor, int position, AbstractNode n )
    {
        List<int>? matchPos = null;
        if( _finderInfo.TriviaMatcher == null )
        {
            if( !HandleMatchCount( monitor, ref matchPos, int.MaxValue ) ) return null;
        }
        else
        {
            int idx = 0;
            foreach( var t in n.LeadingTrivias )
            {
                if( _finderInfo.TriviaMatcher( t ) && !HandleMatchCount( monitor, ref matchPos, idx ) && _hasError ) return null;
                ++idx;
            }
            idx = 0;
            foreach( var t in n.TrailingTrivias )
            {
                if( _finderInfo.TriviaMatcher( t ) && !HandleMatchCount( monitor, ref matchPos, ~idx ) && _hasError ) return null;
                ++idx;
            }
            if( matchPos == null ) return null;
        }
        MatchedNode m = new MatchedNode( position, n, matchPos );
        if( _lastBuffer != null )
        {
            _lastBuffer.Push( m );
            return null;
        }
        return m;
    }

    bool HandleMatchCount( IActivityMonitor monitor, ref List<int>? matchPos, int idx = int.MaxValue )
    {
        if( ++_matchCount > 1 && (_finderInfo.Card.ExpectedMatchCount > 0 && _matchCount > _finderInfo.Card.ExpectedMatchCount) )
        {
            monitor.Error( $"Too many matches found for (max is {_finderInfo.Card.ExpectedMatchCount})." );
            _hasError = true;
        }
        else if( !_finderInfo.Card.FromFirst || (_finderInfo.Card.All || _matchCount == _finderInfo.Card.Offset + 1) )
        {
            if( idx != int.MaxValue )
            {
                matchPos ??= new List<int>();
                matchPos.Add( idx );
            }
            return true;
        }
        return false;
    }

    public MatchedNode? Conclude()
    {
        Throw.DebugAssert( "Conclude is not called when FromFirst...", !_finderInfo.Card.FromFirst );
        Throw.DebugAssert( "... we then have the buffer.", _lastBuffer != null );
        if( _matchCount < _lastBuffer.Capacity ) return null;
        if( _finderInfo.TriviaMatcher == null )
        {
            return _lastBuffer.PeekLast();
        }
        int targetIdxFromLast = _lastBuffer.Capacity - 1;
        int iNode = _lastBuffer.Count - 1;
        Throw.DebugAssert( "We have a TriviaMatcher.", _lastBuffer[iNode].IdxTrivias != null );
        MatchedNode m;
        while( (m = _lastBuffer[iNode]).IdxTrivias!.Count <= targetIdxFromLast )
        {
            targetIdxFromLast -= m.IdxTrivias.Count;
            --iNode;
            Throw.DebugAssert( "We have a TriviaMatcher.", _lastBuffer[iNode].IdxTrivias != null );
        }
        if( m.IdxTrivias.Count == 1 ) return m;
        return new MatchedNode( m.Position, m.Node, new[] { m.IdxTrivias[m.IdxTrivias.Count - 1 - targetIdxFromLast] } );
    }

}
