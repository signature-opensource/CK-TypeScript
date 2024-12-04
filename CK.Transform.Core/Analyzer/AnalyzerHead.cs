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
    IAnalyzerBehavior _behavior;
    TriviaParser _triviaParser;
    ImmutableArray<Trivia> _leadingTrivias;
    TokenErrorNode? _triviaError;
    TokenErrorNode? _finalError;
    ReadOnlySpan<char> _lowLevelTokenText;
    LowLevelToken _lowLevelToken;
    int _lastSuccessfulHead;

    internal AnalyzerHead( Analyzer analyzer )
    {
        _memText = analyzer.RemainingText;
        _text = _memText.Span;
        _head = _text;
        _triviaBuilder = analyzer._triviaBuilder;
        _behavior = analyzer;
        _triviaParser = analyzer.ParseTrivia;
        EnsureLeadingTrivias();
    }

    public AnalyzerHead( ReadOnlyMemory<char> text,
                         IAnalyzerBehavior behavior,
                         ImmutableArray<Trivia>.Builder? triviaBuilder = null )
    {
        _memText = text;
        _text = _memText.Span;
        _head = _text;
        _triviaBuilder = triviaBuilder ?? ImmutableArray.CreateBuilder<Trivia>();
        _behavior = behavior;
        _triviaParser = behavior.ParseTrivia;
        EnsureLeadingTrivias();
    }

    public AnalyzerHead( ref AnalyzerHead from, IAnalyzerBehavior? behavior = null )
    {
        _memText = from.GetRemainingText();
        _text = _memText.Span;
        _head = _text;
        _triviaBuilder = from._triviaBuilder;
        if( behavior != null )
        {
            _behavior = behavior;
            _triviaParser = behavior.ParseTrivia;
        }
        else
        {
            _behavior = from._behavior;
            _triviaParser = from._triviaParser;
        }
        EnsureLeadingTrivias();
    }

    /// <summary>
    /// Gets the current head to analyze.
    /// </summary>
    public readonly ReadOnlySpan<char> Head => _head;

    /// <summary>
    /// Gets the remaining text.
    /// </summary>
    /// <returns></returns>
    public readonly ReadOnlyMemory<char> GetRemainingText() => _memText.Slice( _lastSuccessfulHead );

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
    /// Gets the low-level token.
    /// </summary>
    public LowLevelToken LowLevelToken => _lowLevelToken;

    /// <summary>
    /// Gets the <see cref="LowLevelToken"/> text.
    /// </summary>
    public ReadOnlySpan<char> LowLevelTokenText => _lowLevelTokenText;

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
    public void AcceptToken( int tokenLength,
                             out ReadOnlyMemory<char> text,
                             out ImmutableArray<Trivia> leading,
                             out ImmutableArray<Trivia> trailing )
    {
        Throw.CheckArgument( tokenLength > 0 );
        Throw.CheckState( TriviaError == null );
        // If a "ManualTriviaMode" is implemented, this must become a Throw.CheckState.
        Throw.DebugAssert( !_leadingTrivias.IsDefault );
        text = _memText.Slice( _text.Length - _head.Length, tokenLength );
        _head = _head.Slice( tokenLength );
        leading = _leadingTrivias;
        _leadingTrivias = default;
        trailing = GetTrailingTrivias();
        // Before preloading the leading trivia for the next token, save the
        // current head position. RemainingText is based on this index.
        _lastSuccessfulHead = _head.Length;
        EnsureLeadingTrivias();
    }

    /// <summary>
    /// Accepts the current <see cref="Head"/> and creates a basic <see cref="TokenNode"/> of the <paramref name="type"/>
    /// and forwards the head.
    /// <para>
    /// If a <see cref="FinalError"/> exists, it is returned instead (once a final error is set, no token can be accepted).
    /// </para>
    /// </summary>
    /// <param name="type">The <see cref="TokenNode.NodeType"/> to create.</param>
    /// <param name="tokenLength">The length of the token. Must be positive.</param>
    /// <returns>The token node.</returns>
    public TokenNode CreateToken( NodeType type, int tokenLength )
    {
        if( FinalError != null ) return FinalError;
        Throw.CheckArgument( !type.IsError() &&  !type.IsTrivia() );
        AcceptToken( tokenLength, out var text, out var leading, out var trailing );
        // Use the internal unchecked constructor as every parameters have been checked.
        return new TokenNode( leading, trailing, type, text );
    }

    /// <summary>
    /// Accepts the <see cref="LowLevelToken"/>, creates a basic <see cref="TokenNode"/>and forwards the head.
    /// <para>
    /// If a <see cref="FinalError"/> exists, it is returned instead (once a final error is set, no token can be accepted).
    /// </para>
    /// </summary>
    /// <param name="type">The token type to create. Defaults to <see cref="LowLevelToken.NodeType"/>.</param>
    /// <returns>The token node.</returns>
    public TokenNode CreateLowLevelToken( NodeType type = NodeType.None )
    {
        Throw.CheckState( LowLevelToken.Length > 0 );
        if( type == NodeType.None ) type = _lowLevelToken.NodeType;
        return CreateToken( type, _lowLevelToken.Length );
    }

    /// <summary>
    /// Helper function for easy case that matches the <see cref="LowLevelTokenText"/> and
    /// creates a <see cref="TokenNode"/> on success.
    /// </summary>
    /// <param name="expectedText">The text that must match the <see cref="LowLevelTokenText"/>. Must not be empty.</param>
    /// <param name="result">The non null TokenNode on success.</param>
    /// <param name="type">The token type to create. Defaults to <see cref="LowLevelToken.NodeType"/>.</param>
    /// <param name="comparisonType">Optional comparison type.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool AcceptLowLevelToken( ReadOnlySpan<char> expectedText,
                                     [NotNullWhen( true )] out TokenNode? result,
                                     NodeType type = NodeType.None,
                                     StringComparison comparisonType = StringComparison.Ordinal )
    {
        Throw.CheckArgument( expectedText.Length > 0 );
        if( _lowLevelTokenText.Equals( expectedText, comparisonType ) )
        {
            if( type == NodeType.None ) type = _lowLevelToken.NodeType;
            result = CreateToken( type, expectedText.Length );
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
    public readonly TokenErrorNode CreateError( string errorMessage, NodeType errorType = NodeType.SyntaxErrorNode|NodeType.ErrorClassBit )
    {
        Throw.CheckArgument( errorType.IsError() );
        Throw.CheckArgument( !string.IsNullOrWhiteSpace( errorMessage ) );
        return new TokenErrorNode( errorType, errorMessage, CreateSourcePosition(), _leadingTrivias, ImmutableArray<Trivia>.Empty );
    }

    /// <summary>
    /// Matches the <see cref="LowLevelTokenText"/> and return a <see cref="TokenNode"/> on success
    /// or a <see cref="TokenErrorNode"/> on failure.
    /// </summary>
    /// <param name="expected"></param>
    /// <param name="type">The token type to create. Defaults to <see cref="LowLevelToken.NodeType"/>.</param>
    /// <param name="newBehavior">Optional behavior that will be set after the parse.</param>
    /// <returns></returns>
    public TokenNode MatchToken( ReadOnlySpan<char> expected,
                                 NodeType type = NodeType.None,
                                 StringComparison comparisonType = StringComparison.Ordinal,
                                 IAnalyzerBehavior? newBehavior = null )
    {
        if( AcceptLowLevelToken( expected, out var n, type, comparisonType ) ) return n;
        return CreateError( $"Expected '{expected}'." );
    }


    #region Internal & private

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
                _finalError = _triviaError = CreateError("End of input.", NodeType.EndOfInput);
                return false;
            }
            // Resets the current low-level token.
            _lowLevelToken = default;
            // Creates the Trivia head and collects every possible trivias thanks to the
            // current trivia parser.
            var c = new TriviaHead( _head, _memText, _triviaBuilder );
            c.ParseAll( _triviaParser );
            // Captures the collected trivias: CreateError will have them.
            _leadingTrivias = _triviaBuilder.DrainToImmutable();
            // Forwards the head: on error, the SourcePosition will be accurate.
            _head = _head.Slice( c.AcceptedLength );
            // On trivia error, skips low-level token detection. 
            if( c.HasError )
            {
                _finalError = _triviaError = CreateError("Missing comment end.", c.Error);
                return false;
            }
            if( _head.Length == 0 )
            {
                _finalError = _triviaError = CreateError("End of input.", NodeType.EndOfInput);
                return false;
            }
            // Initializes the low-level token.
            _lowLevelToken = _behavior.LowLevelTokenize( _head );
            Throw.CheckState( _lowLevelToken.Length >= 0  );
            _lowLevelTokenText = _head.Slice( 0, _lowLevelToken.Length );
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
            _finalError = _triviaError = CreateError( "End of input.", NodeType.EndOfInput );
            return ImmutableArray<Trivia>.Empty;
        }
        var c = new TriviaHead( _head, _memText, _triviaBuilder );
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

