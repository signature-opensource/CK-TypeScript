using CK.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Analyzer head that can be extended by extension methods to create specialized <see cref="TokenNode"/>.
/// Extension methods can also be used to expose <see cref="AbstractNode"/> factory methods.
/// </summary>
public ref struct AnalyzerHead
{
    ReadOnlySpan<char> _head;
    readonly ReadOnlySpan<char> _text;
    readonly ReadOnlyMemory<char> _memText;
    readonly ImmutableArray<Trivia>.Builder _triviaBuilder;
    TriviaParser? _triviaParser;
    ImmutableArray<Trivia> _leadingTrivias;
    TokenErrorNode? _triviaError;
    TokenErrorNode? _finalError;
    int _lastSuccessfulHead;

    internal AnalyzerHead( Analyzer analyzer )
    {
        _memText = analyzer.RemainingText;
        _text = _memText.Span;
        _head = _text;
        _triviaBuilder = analyzer._triviaBuilder;
        _triviaParser = analyzer.ParseTrivia;
    }

    /// <summary>
    /// Gets the current head to analyze.
    /// </summary>
    public readonly ReadOnlySpan<char> Head => _head;

    /// <summary>
    /// Gets the error from the trivia analysis.
    /// If it exists, it is necessarily the <see cref="FinalError"/>.
    /// </summary>
    public readonly TokenErrorNode? TriviaError => _triviaError;

    /// <summary>
    /// Gets the final error.
    /// Once set (either because a <see cref="TriviaError"/> has been encountered) or because <see cref="SetFinalError"/> has been called,
    /// no new <see cref="TokenNode"/> can be created: <see cref="AcceptToken(int, out ReadOnlyMemory{char}, out ImmutableArray{Trivia}, out ImmutableArray{Trivia})"/>
    /// cannot be called anymore.
    /// </summary>
    public readonly TokenErrorNode? FinalError => _finalError;

    /// <summary>
    /// Sets a final error. Must not be called if <see cref="FinalError"/> is not null.
    /// <para>
    /// As its name suggests, once set no more token can be accepted: <see cref="AcceptToken(int, out ReadOnlyMemory{char}, out ImmutableArray{Trivia}, out ImmutableArray{Trivia})"/>
    /// will throw an <see cref="InvalidOperationException"/>.
    /// </para>
    /// </summary>
    /// <param name="error"></param>
    public void SetFinalError( TokenErrorNode error )
    {
        Throw.CheckNotNullArgument( error );
        if( _finalError != null && _finalError != error )
        {
            Throw.InvalidOperationException( "A final error has already been set." );
        }
        _finalError = error;
    }

    /// <summary>
    /// Allows to replace the default trivia parser that is the <see cref="Analyzer.ParseTrivia(ref TriviaHead)"/>.
    /// This can be used in advanced scenario where trivias can change during an analysis.
    /// <para>
    /// Setting null handles only <see cref="TokenType.Whitespace"/> trivias.
    /// </para>
    /// </summary>
    /// <param name="parser">The trivia parser to use.</param>
    /// <returns>The previous parser (can be restored later).</returns>
    public TriviaParser? SetTriviaParser( TriviaParser? parser )
    {
        var previous = _triviaParser;
        _triviaParser = parser;
        return previous;
    }

    /// <summary>
    /// Validates the <see cref="Head"/> first <paramref name="tokenLength"/> characters. A <see cref="TokenNode"/> should be
    /// created with the final <paramref name="text"/>, <paramref name="leading"/> and <paramref name="trailing"/> data.
    /// <para>
    /// This must not be call if a <see cref="FinalError"/> exists: the final error is the only token node that can exist. 
    /// </para>
    /// </summary>
    /// <param name="tokenLength">The length of the token. Must be positive.</param>
    /// <param name="text">The resulting <see cref="TokenNode.Text"/>.</param>
    /// <param name="leading">The resulting <see cref="AbstractNode.LeadingNodes"/>.</param>
    /// <param name="trailing">The resulting <see cref="AbstractNode.TrailingNodes"/>.</param>
    public void AcceptToken( int tokenLength, out ReadOnlyMemory<char> text, out ImmutableArray<Trivia> leading, out ImmutableArray<Trivia> trailing )
    {
        Throw.CheckArgument( tokenLength > 0 );
        Throw.CheckState( TriviaError == null );
        // If a "ManualTriviaMode" is implemented, this must become a Throw.CheckState.
        Throw.DebugAssert( !_leadingTrivias.IsDefault );
        text = _memText.Slice( _text.Length - _head.Length, tokenLength );
        _head = _head.Slice( tokenLength );
        leading = _leadingTrivias;
        // Before preloading the leading trivia for the next token, save the
        // current head position. The calling Analyzer updates its 
        // RemainingText from this index.
        _lastSuccessfulHead = _head.Length;
        _leadingTrivias = default;
        trailing = GetTrailingTrivias();
        EnsureLeadingTrivias();
    }

    /// <summary>
    /// Accepts the current <see cref="Head"/> and creates a basic <see cref="TokenNode"/> of the <paramref name="type"/>
    /// and forwards it accordingly.
    /// <para>
    /// If a <see cref="FinalError"/> exists, it is returned instead (once a final error is set, no token can be accepted).
    /// </para>
    /// </summary>
    /// <param name="type">The <see cref="TokenNode.TokenType"/> to create.</param>
    /// <param name="tokenLength">The length of the token. Must be positive.</param>
    /// <returns>The token node.</returns>
    public TokenNode AcceptToken( TokenType type, int tokenLength )
    {
        if( FinalError != null ) return FinalError;
        Throw.CheckArgument( !type.IsError() &&  !type.IsTrivia() );
        AcceptToken( tokenLength, out var text, out var leading, out var trailing );
        // Use the internal unchecked constructor as every parameters have been checked.
        return new TokenNode( leading, trailing, type, text );
    }

    /// <summary>
    /// Helper function for easy case that matches the start of the <see cref="Head"/>
    /// and forwards it on success.
    /// </summary>
    /// <param name="type">The token type.</param>
    /// <param name="text">The text that must match the start of the <see cref="Head"/>. Must not be empty.</param>
    /// <param name="result">The non null TokenNode on success.</param>
    /// <param name="comparisonType">Optional comparison type.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool TryAcceptToken( TokenType type,
                                ReadOnlySpan<char> text,
                                [NotNullWhen( true )] out TokenNode? result,
                                StringComparison comparisonType = StringComparison.Ordinal )
    {
        Throw.CheckArgument( text.Length > 0 );
        if( _head.StartsWith( text, comparisonType ) )
        {
            result = AcceptToken( type, text.Length );
            return true;
        }
        result = null;
        return false;
    }

    /// <summary>
    /// Creates a token error node at the current <see cref="Head"/> position.
    /// <para>
    /// This can be called anytime (this is independent of <see cref="TriviaError"/> and <see cref="FinalError"/>).
    /// </para>
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorType">The error token type.</param>
    /// <returns>A token error node.</returns>
    public readonly TokenErrorNode CreateError( string errorMessage, TokenType errorType = TokenType.SyntaxError )
    {
        Throw.CheckArgument( errorType.IsError() );
        Throw.CheckArgument( !string.IsNullOrWhiteSpace( errorMessage ) );
        return new TokenErrorNode( errorType, errorMessage, CreateSourcePosition(), _leadingTrivias, ImmutableArray<Trivia>.Empty );
    }

    #region Internal & private

    internal readonly ReadOnlyMemory<char> GetRemainingText() => _memText.Slice( _lastSuccessfulHead );

    /// <summary>
    /// Currently private but may be exposed with a "ManualTriviaMode" configuration
    /// if needed... but I doubt it.
    /// <para>
    /// This is idempotent.
    /// The leading trivias are eventually consumed
    /// by <see cref="AcceptToken(int, out ReadOnlyMemory{char}, out ImmutableArray{Trivia}, out ImmutableArray{Trivia})"/>
    /// </para>
    /// </summary>
    /// <returns>True on sucess, false if <see cref="FinalError"/> is already set or has been set by this call.</returns>
    bool EnsureLeadingTrivias()
    {
        if( _leadingTrivias.IsDefault )
        {
            if( _finalError != null )
            {
                _leadingTrivias = ImmutableArray<Trivia>.Empty;
                return false;
            }
            if( _head.Length == 0 )
            {
                _leadingTrivias = ImmutableArray<Trivia>.Empty;
                _finalError = _triviaError = CreateError("End of input.", TokenType.EndOfInput);
                return false;
            }
            var c = new TriviaHead( _head, _head.Length, _memText, _triviaBuilder );
            c.ParseAll( _triviaParser );
            // Captures the collected trivias: CreateError will have them.
            _leadingTrivias = _triviaBuilder.DrainToImmutable();
            // Forwards the head: on error, the SourcePosition will be accurate.
            _head = _head.Slice( c.AcceptedLength );
            if( c.HasError )
            {
                _finalError = _triviaError = CreateError("Missing comment end.", c.Error);
                return false;
            }
            if( _head.Length == 0 )
            {
                _finalError = _triviaError = CreateError("End of input.", TokenType.EndOfInput);
                return false;
            }
        }
        return true;
    }

    ImmutableArray<Trivia> GetTrailingTrivias()
    {
        if( _finalError != null )
        {
            return ImmutableArray<Trivia>.Empty;
        }
        if( _head.Length == 0 )
        {
            _finalError = _triviaError = CreateError( "End of input.", TokenType.EndOfInput );
            return ImmutableArray<Trivia>.Empty;
        }
        var c = new TriviaHead( _head, _head.Length, _memText, _triviaBuilder );
        c.ParseTrailingTrivias( _triviaParser );
        var trivias = _triviaBuilder.DrainToImmutable();
        // Forwards the head: on error, the SourcePosition will be accurate.
        _head = _head.Slice( c.AcceptedLength );
        if( c.HasError )
        {
            _finalError = _triviaError = CreateError( "Missing comment end.", c.Error );
        }
        return trivias;
    }

    readonly SourcePosition CreateSourcePosition() => SourcePosition.GetSourcePosition( _text, _text.Length - _head.Length );

    #endregion
}

