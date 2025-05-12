using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CK.Transform.Core;

struct FilteredTokenSpanEnumeratorImpl
{
    IReadOnlyList<FilteredTokenSpan> _matches;
    IReadOnlyList<Token> _tokens;
    Token? _token;
    int _mIndex;
    int _tIndex;
    FilteredTokenSpanEnumeratorState _state;

    /// <summary>
    /// Initializes a new enumerator.
    /// </summary>
    /// <param name="matches">The filtered tokens.</param>
    /// <param name="tokens">The source code tokens.</param>
    public FilteredTokenSpanEnumeratorImpl( IReadOnlyList<FilteredTokenSpan> matches, IReadOnlyList<Token> tokens )
    {
        _matches = matches;
        _tokens = tokens;
        _tIndex = -1;
    }

    [Conditional( "DEBUG" )]
    void CheckInvariants( bool checkMatches = false )
    {
        if( checkMatches )
        {
            _matches.CheckInvariants( _tokens );
        }
        switch( _state )
        {
            case FilteredTokenSpanEnumeratorState.Unitialized:
                Throw.CheckState( _tIndex == -1 && _token == null );
                Throw.CheckState( _mIndex == 0 );
                break;
            case FilteredTokenSpanEnumeratorState.Each:
            case FilteredTokenSpanEnumeratorState.Match:
                Throw.CheckState( _tIndex == -1 && _token == null );
                Throw.CheckState( _mIndex >= 0 && _mIndex < _matches.Count );
                break;
            case FilteredTokenSpanEnumeratorState.Token:
                Throw.CheckState( _mIndex >= 0 && _mIndex < _matches.Count );
                Throw.CheckState( _tIndex >= 0 && _token == _tokens[_tIndex] );
                break;
            case FilteredTokenSpanEnumeratorState.Finished:
                Throw.CheckState( _tIndex == -1 && _token == null );
                Throw.CheckState( _mIndex == _matches.Count );
                break;
            default:
                Throw.NotSupportedException();
                break;
        }
    }

    public bool IsEmpty => _matches.Count == 0;

    public bool IsSingleEach => _matches.Count > 0 && _matches[^1].EachIndex == 0;

    public FilteredTokenSpanEnumeratorState State => _state;

    public FilteredTokenSpan CurrentMatch
    {
        get
        {
            Throw.CheckState( State is FilteredTokenSpanEnumeratorState.Match or FilteredTokenSpanEnumeratorState.Token );
            return _matches[_mIndex];
        }
    }

    public SourceToken Token
    {
        get
        {
            Throw.CheckState( State == FilteredTokenSpanEnumeratorState.Token );
            return new SourceToken( _token!, _tIndex );
        }
    }

    public bool NextEach()
    {
        CheckInvariants( true );
        if( _state is FilteredTokenSpanEnumeratorState.Finished )
        {
            return false;
        }
        if( _state is FilteredTokenSpanEnumeratorState.Unitialized )
        {
            if( _matches.Count > 0 )
            {
                _state = FilteredTokenSpanEnumeratorState.Each;
                CheckInvariants();
                return true;
            }
            _state = FilteredTokenSpanEnumeratorState.Finished;
            CheckInvariants();
            return false;
        }
        _tIndex = -1;
        _token = null;
        int currentEach = _matches[_mIndex].EachIndex;
        FilteredTokenSpan nextMatch;
        do
        {
            ++_mIndex;
            if( _mIndex == _matches.Count )
            {
                _state = FilteredTokenSpanEnumeratorState.Finished;
                CheckInvariants();
                return false;
            }
            nextMatch = _matches[_mIndex];
        }
        while( nextMatch.EachIndex == currentEach );
        _state = FilteredTokenSpanEnumeratorState.Each;
        CheckInvariants();
        return true;
    }

    public bool NextMatch()
    {
        CheckInvariants( true );
        if( _state is FilteredTokenSpanEnumeratorState.Finished )
        {
            return false;
        }
        Throw.CheckState( _state is not FilteredTokenSpanEnumeratorState.Unitialized );
        Throw.DebugAssert( _state is FilteredTokenSpanEnumeratorState.Each or FilteredTokenSpanEnumeratorState.Match or FilteredTokenSpanEnumeratorState.Token );
        _tIndex = -1;
        _token = null;
        if( _state is FilteredTokenSpanEnumeratorState.Each )
        {
            _state = FilteredTokenSpanEnumeratorState.Match;
            CheckInvariants();
            return true;
        }
        int nextMatchIndex = _mIndex + 1;
        if( nextMatchIndex == _matches.Count )
        {
            _state = FilteredTokenSpanEnumeratorState.Finished;
            CheckInvariants();
            return false;
        }
        FilteredTokenSpan nextMatch = _matches[nextMatchIndex];
        if( nextMatch.EachIndex != _matches[_mIndex].EachIndex )
        {
            CheckInvariants();
            return false;
        }
        _mIndex = nextMatchIndex;
        _state = FilteredTokenSpanEnumeratorState.Match;
        CheckInvariants();
        return true;
    }

    public bool NextToken()
    {
        CheckInvariants( true );
        if( _state is FilteredTokenSpanEnumeratorState.Finished )
        {
            return false;
        }
        Throw.CheckState( _state is FilteredTokenSpanEnumeratorState.Match or FilteredTokenSpanEnumeratorState.Token );
        if( _state is FilteredTokenSpanEnumeratorState.Match )
        {
            _tIndex = _matches[_mIndex].Span.Beg;
            _token = _tokens[_tIndex];
            _state = FilteredTokenSpanEnumeratorState.Token;
            CheckInvariants();
            return true;
        }
        var nextTokenIndex = _tIndex + 1;
        Throw.DebugAssert( nextTokenIndex <= CurrentMatch.Span.End );
        if( nextTokenIndex < _matches[_mIndex].Span.End )
        {
            _tIndex = nextTokenIndex;
            _token = _tokens[nextTokenIndex];
            CheckInvariants();
            return true;
        }
        CheckInvariants();
        return false;
    }

    internal void Reset( IReadOnlyList<FilteredTokenSpan> matches, IReadOnlyList<Token> tokens )
    {
        _matches = matches;
        _tokens = tokens;
        _state = FilteredTokenSpanEnumeratorState.Unitialized;
        _mIndex = 0;
        _tIndex = -1;
        _token = null;
    }

}


