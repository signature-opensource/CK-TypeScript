using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CK.Transform.Core;

struct TokenFilterEnumeratorImpl
{
    TokenMatch[] _input;
    IReadOnlyList<Token> _tokens;
    Token? _token;
    int _mIndex;
    int _tIndex;
    TokenFilterEnumeratorState _state;

    public TokenFilterEnumeratorImpl( TokenMatch[] input, IReadOnlyList<Token> tokens )
    {
        _input = input;
        _tokens = tokens;
        _tIndex = -1;
        _state = input.Length == 0
                    ? TokenFilterEnumeratorState.Finished
                    : TokenFilterEnumeratorState.Unitialized;
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
            case TokenFilterEnumeratorState.Unitialized:
                Throw.CheckState( _tIndex == -1 && _token == null );
                Throw.CheckState( _mIndex == 0 );
                break;
            case TokenFilterEnumeratorState.Each:
            case TokenFilterEnumeratorState.Match:
                Throw.CheckState( _mIndex >= 0 && _mIndex < _input.Length );
                break;
            case TokenFilterEnumeratorState.Token:
                Throw.CheckState( _mIndex >= 0 && _mIndex < _input.Length );
                Throw.CheckState( _tIndex >= 0 && _token == _tokens[_tIndex] );
                break;
            case TokenFilterEnumeratorState.Finished:
                Throw.CheckState( _tIndex == -1 && _token == null );
                Throw.CheckState( _mIndex == _input.Length );
                break;
            default:
                Throw.NotSupportedException();
                break;
        }
    }

    public bool IsSingleEach => _input.Length > 0 && _input[^1].EachIndex == 0;

    public TokenFilterEnumeratorState State => _state;

    public IReadOnlyList<TokenMatch> Input => _input;

    public int CurrentInputIndex => _mIndex;

    public int CurrentEachIndex
    {
        get
        {
            Throw.CheckState( State is not TokenFilterEnumeratorState.Finished );
            return _input[_mIndex].EachIndex;
        }
    }

    public TokenMatch CurrentMatch
    {
        get
        {
            Throw.CheckState( State is TokenFilterEnumeratorState.Match or TokenFilterEnumeratorState.Token );
            return _input[_mIndex];
        }
    }

    public SourceToken Token
    {
        get
        {
            Throw.CheckState( State == TokenFilterEnumeratorState.Token );
            return new SourceToken( _token!, _tIndex );
        }
    }

    public bool NextEach()
    {
        if( _state is TokenFilterEnumeratorState.Finished )
        {
            return false;
        }
        if( _state is TokenFilterEnumeratorState.Unitialized )
        {
            Throw.DebugAssert( _input.Length > 0 );
            _state = TokenFilterEnumeratorState.Each;
            CheckInvariants();
            return true;
        }
        // Wahtever happens now, we lose any current token.
        _tIndex = -1;
        _token = null;
        int currentEach = _input[_mIndex].EachIndex;
        TokenMatch nextMatch;
        do
        {
            ++_mIndex;
            if( _mIndex == _input.Length )
            {
                _state = TokenFilterEnumeratorState.Finished;
                CheckInvariants();
                return false;
            }
            nextMatch = _input[_mIndex];
        }
        while( nextMatch.EachIndex == currentEach );
        _state = TokenFilterEnumeratorState.Each;
        CheckInvariants();
        return true;
    }

    public bool NextMatch()
    {
        if( _state is TokenFilterEnumeratorState.Finished )
        {
            return false;
        }
        Throw.CheckState( _state is not TokenFilterEnumeratorState.Unitialized );
        return DoNextMatch();
    }

    public bool NextMatch( out SourceToken first, out SourceToken last, out int count )
    {
        Throw.CheckState( State is not TokenFilterEnumeratorState.Unitialized );
        if( _state is TokenFilterEnumeratorState.Finished
            || !DoNextMatch() )
        {
            first = default;
            last = default;
            count = 0;
            return false;
        }
        var m = _input[_mIndex];
        int beg = m.Span.Beg;
        int end = m.Span.End;
        count = end - beg;
        first = new SourceToken( _tokens[beg], beg );
        last = count == 1
                ? first
                : new SourceToken( _tokens[--end], end );
        _token = last.Token;
        _tIndex = last.Index;
        _state = TokenFilterEnumeratorState.Token;
        return true;
    }

    bool DoNextMatch()
    {
        Throw.DebugAssert( _state is TokenFilterEnumeratorState.Each or TokenFilterEnumeratorState.Match or TokenFilterEnumeratorState.Token );
        if( _state is TokenFilterEnumeratorState.Each )
        {
            _tIndex = -1;
            _token = null;
            _state = TokenFilterEnumeratorState.Match;
            CheckInvariants();
            return true;
        }
        int nextMatchIndex = _mIndex + 1;
        if( nextMatchIndex == _input.Length )
        {
            _mIndex = nextMatchIndex;
            _tIndex = -1;
            _token = null;
            _state = TokenFilterEnumeratorState.Finished;
            CheckInvariants();
            return false;
        }
        // If we block on the next each, don't change any state (keep
        // the current token if any).
        TokenMatch nextMatch = _input[nextMatchIndex];
        if( nextMatch.EachIndex != _input[_mIndex].EachIndex )
        {
            CheckInvariants();
            return false;
        }
        _mIndex = nextMatchIndex;
        _tIndex = -1;
        _token = null;
        _state = TokenFilterEnumeratorState.Match;
        CheckInvariants();
        return true;
    }

    public bool NextToken()
    {
        if( _state is TokenFilterEnumeratorState.Finished )
        {
            return false;
        }
        Throw.CheckState( _state is TokenFilterEnumeratorState.Match or TokenFilterEnumeratorState.Token );
        if( _state is TokenFilterEnumeratorState.Match )
        {
            _tIndex = _input[_mIndex].Span.Beg;
            _token = _tokens[_tIndex];
            _state = TokenFilterEnumeratorState.Token;
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

    internal void Reset( TokenMatch[] input, IReadOnlyList<Token> tokens )
    {
        _input = input;
        _tokens = tokens;
        _mIndex = 0;
        _tIndex = -1;
        _token = null;
        _state = input.Length == 0
                    ? TokenFilterEnumeratorState.Finished
                    : TokenFilterEnumeratorState.Unitialized;
        CheckInvariants( true );
    }

    internal void OnUpdateTokens( int eLimit, int delta )
    {
        Throw.DebugAssert( "Must not be called on finished state.", _state is not TokenFilterEnumeratorState.Finished );
        // If we are on a token, consider it observed.
        if( _tIndex >= 0 )
        {
            if( eLimit > _tIndex )
            {
                ThrowUnobserved( eLimit, _tIndex );
            }
        }
        else
        {
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
            Throw.DebugAssert( _state is TokenFilterEnumeratorState.Each or TokenFilterEnumeratorState.Match );
            int lastObsterved = _input[_mIndex - 1].Span.End - 1;
            if( eLimit > lastObsterved )
            {
                ThrowUnobserved( eLimit, lastObsterved );
                return;
            }
        }
        // Adjust
        if( _tIndex >= 0 )
        {
            Throw.DebugAssert( _token != null );
            _tIndex += delta;
            _token = _tokens[_tIndex];
        }
        var adjust = _input.AsSpan( _mIndex );
        for( int i = 0; i < adjust.Length; ++i )
        {
            ref var m = ref adjust[i];
            int newBeg = Math.Max( m.Span.Beg + delta, 0 );
            m = new TokenMatch( m.EachIndex, m.MatchIndex, new TokenSpan( newBeg, m.Span.End + delta ) );
        }
        // The match before _mIndex may no more be valid anymore: there may be more than one match
        // whose Beg has been clamped to 0 (so they overlap): the _input is no more
        // globally valid, only the enumerator state can be checked.
        CheckInvariants();

        static void ThrowUnobserved( int eLimit, int observed )
        {
            Throw.CKException( $"Token has not been observed at {eLimit} (current is {observed})." );
        }
    }

}


