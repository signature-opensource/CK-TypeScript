using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CK.Transform.Core;

struct FilteredTokenSpanEnumeratorImpl
{
    FilteredTokenSpan[] _input;
    IReadOnlyList<Token> _tokens;
    Token? _token;
    int _mIndex;
    int _tIndex;
    FilteredTokenSpanEnumeratorState _state;

    public FilteredTokenSpanEnumeratorImpl( FilteredTokenSpan[] input, IReadOnlyList<Token> tokens )
    {
        _input = input;
        _tokens = tokens;
        _tIndex = -1;
        _state = input.Length == 0
                    ? FilteredTokenSpanEnumeratorState.Finished
                    : FilteredTokenSpanEnumeratorState.Unitialized;
        CheckInvariants( true );
    }

    [Conditional( "DEBUG" )]
    void CheckInvariants( bool checkInput = false )
    {
        if( checkInput )
        {
            if( !_input.CheckValid( _tokens, out var error ) )
            {
                Throw.InvalidOperationException( error );
            }
        }
        switch( _state )
        {
            case FilteredTokenSpanEnumeratorState.Unitialized:
                Throw.CheckState( _tIndex == -1 && _token == null );
                Throw.CheckState( _mIndex == 0 );
                break;
            case FilteredTokenSpanEnumeratorState.Each:
            case FilteredTokenSpanEnumeratorState.Match:
                Throw.CheckState( _mIndex >= 0 && _mIndex < _input.Length );
                break;
            case FilteredTokenSpanEnumeratorState.Token:
                Throw.CheckState( _mIndex >= 0 && _mIndex < _input.Length );
                Throw.CheckState( _tIndex >= 0 && _token == _tokens[_tIndex] );
                break;
            case FilteredTokenSpanEnumeratorState.Finished:
                Throw.CheckState( _tIndex == -1 && _token == null );
                Throw.CheckState( _mIndex == _input.Length );
                break;
            default:
                Throw.NotSupportedException();
                break;
        }
    }

    public bool IsEmpty => _input.Length == 0;

    public bool IsSingleEach => _input.Length > 0 && _input[^1].EachIndex == 0;

    public FilteredTokenSpanEnumeratorState State => _state;

    public IReadOnlyList<FilteredTokenSpan> Input => _input;

    public int CurrentInputIndex => _mIndex;

    public int CurrentEachIndex
    {
        get
        {
            Throw.CheckState( State is not FilteredTokenSpanEnumeratorState.Finished );
            return _input[_mIndex].EachIndex;
        }
    }

    public FilteredTokenSpan CurrentMatch
    {
        get
        {
            Throw.CheckState( State is FilteredTokenSpanEnumeratorState.Match or FilteredTokenSpanEnumeratorState.Token );
            return _input[_mIndex];
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
        if( _state is FilteredTokenSpanEnumeratorState.Finished )
        {
            return false;
        }
        if( _state is FilteredTokenSpanEnumeratorState.Unitialized )
        {
            Throw.DebugAssert( _input.Length > 0 );
            _state = FilteredTokenSpanEnumeratorState.Each;
            CheckInvariants();
            return true;
        }
        // Wahtever happens now, we lose any current token.
        _tIndex = -1;
        _token = null;
        int currentEach = _input[_mIndex].EachIndex;
        FilteredTokenSpan nextMatch;
        do
        {
            ++_mIndex;
            if( _mIndex == _input.Length )
            {
                _state = FilteredTokenSpanEnumeratorState.Finished;
                CheckInvariants();
                return false;
            }
            nextMatch = _input[_mIndex];
        }
        while( nextMatch.EachIndex == currentEach );
        _state = FilteredTokenSpanEnumeratorState.Each;
        CheckInvariants();
        return true;
    }

    public bool NextMatch()
    {
        if( _state is FilteredTokenSpanEnumeratorState.Finished )
        {
            return false;
        }
        Throw.CheckState( _state is not FilteredTokenSpanEnumeratorState.Unitialized );
        return DoNextMatch();
    }

    public bool NextMatch( out SourceToken currentFirst, out SourceToken currentLast, out int currentCount )
    {
        Throw.CheckState( State is not FilteredTokenSpanEnumeratorState.Unitialized );
        if( _state is FilteredTokenSpanEnumeratorState.Finished )
        {
            currentFirst = default;
            currentLast = default;
            currentCount = 0;
            return false;
        }
        var m = _input[_mIndex];
        int beg = m.Span.Beg;
        int end = m.Span.End;
        currentCount = end - beg;
        currentFirst = new SourceToken( _tokens[beg], beg );
        currentLast = currentCount == 1
                ? currentFirst
                : new SourceToken( _tokens[--end], end );
        return DoNextMatch();
    }

    bool DoNextMatch()
    {
        Throw.DebugAssert( _state is FilteredTokenSpanEnumeratorState.Each or FilteredTokenSpanEnumeratorState.Match or FilteredTokenSpanEnumeratorState.Token );
        if( _state is FilteredTokenSpanEnumeratorState.Each )
        {
            _tIndex = -1;
            _token = null;
            _state = FilteredTokenSpanEnumeratorState.Match;
            CheckInvariants();
            return true;
        }
        int nextMatchIndex = _mIndex + 1;
        if( nextMatchIndex == _input.Length )
        {
            _tIndex = -1;
            _token = null;
            _state = FilteredTokenSpanEnumeratorState.Finished;
            CheckInvariants();
            return false;
        }
        // If we block on the next each, don't change any state (keep
        // the current token if any).
        FilteredTokenSpan nextMatch = _input[nextMatchIndex];
        if( nextMatch.EachIndex != _input[_mIndex].EachIndex )
        {
            CheckInvariants();
            return false;
        }
        _mIndex = nextMatchIndex;
        _tIndex = -1;
        _token = null;
        _state = FilteredTokenSpanEnumeratorState.Match;
        CheckInvariants();
        return true;
    }

    public bool NextToken()
    {
        if( _state is FilteredTokenSpanEnumeratorState.Finished )
        {
            return false;
        }
        Throw.CheckState( _state is FilteredTokenSpanEnumeratorState.Match or FilteredTokenSpanEnumeratorState.Token );
        if( _state is FilteredTokenSpanEnumeratorState.Match )
        {
            _tIndex = _input[_mIndex].Span.Beg;
            _token = _tokens[_tIndex];
            _state = FilteredTokenSpanEnumeratorState.Token;
            CheckInvariants();
            return true;
        }
        var nextTokenIndex = _tIndex + 1;
        Throw.DebugAssert( nextTokenIndex <= CurrentMatch.Span.End );
        if( nextTokenIndex < _input[_mIndex].Span.End )
        {
            _tIndex = nextTokenIndex;
            _token = _tokens[nextTokenIndex];
            CheckInvariants();
            return true;
        }
        CheckInvariants();
        return false;
    }

    internal void Reset( FilteredTokenSpan[] input, IReadOnlyList<Token> tokens )
    {
        _input = input;
        _tokens = tokens;
        _mIndex = 0;
        _tIndex = -1;
        _token = null;
        _state = input.Length == 0
                    ? FilteredTokenSpanEnumeratorState.Finished
                    : FilteredTokenSpanEnumeratorState.Unitialized;
        CheckInvariants( true );
    }

    internal void OnUpdateTokens( int eLimit, int delta )
    {
        Throw.DebugAssert( "Must not be called on finished state.", _state is not FilteredTokenSpanEnumeratorState.Finished );
        // If we are on a token, consider it observed.
        if( _tIndex >= 0 && eLimit > _tIndex )
        {
            ThrowUnobserved( eLimit, _tIndex );
            return;
        }
        // If the enumerator has not been initialized (no NextEach call). Nothing has been observed.
        // If the enumerator has been initialized, we are on a not yet entered match (otherwise we would have a token).
        // 
        // The last observed could be match.Span.Beg - 1. This would allow edits in unmatched ranges.
        // We prefer to be more strict here and retrieve the end of the previous span.
        // Here, if _mIndex is 0, then nothing has been observed.
        //
        if( _mIndex == 0 )
        {
            ThrowUnobserved( eLimit, -1 );
            return;
        }
        // We are on a not yet entered match (otherwise we would have a token) and we are initialized because
        // _mIndex is positive.
        Throw.DebugAssert( _state is FilteredTokenSpanEnumeratorState.Each or FilteredTokenSpanEnumeratorState.Match );
        int lastObsterved = _input[_mIndex - 1].Span.End - 1;
        if( eLimit > lastObsterved )
        {
            ThrowUnobserved( eLimit, lastObsterved );
            return;
        }
        //
        if( _tIndex >= 0 ) _tIndex += delta;
        var adjust = _input.AsSpan( _mIndex - 1 );
        for( int i = 0; i < adjust.Length; ++i )
        {
            ref var m = ref _input[i];
            m = new FilteredTokenSpan( m.EachIndex, m.MatchIndex, new TokenSpan( m.Span.Beg + delta, m.Span.End + delta ) );
        }

        static void ThrowUnobserved( int eLimit, int observed )
        {
            Throw.CKException( $"Token has not been observed at {eLimit} (current is {observed})." );
        }
    }

}


