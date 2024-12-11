using CK.Core;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Parser head that can be extended by extension methods to create specialized <see cref="TokenNode"/>.
/// Extension methods can also be used to expose <see cref="AbstractNode"/> factory methods.
/// </summary>
public ref struct ParserHead
{
    ReadOnlySpan<char> _head;
    readonly ReadOnlySpan<char> _text;
    readonly ReadOnlyMemory<char> _memText;
    readonly ImmutableArray<Trivia>.Builder _triviaBuilder;
    readonly IParserHeadBehavior _behavior;
    TriviaParser _triviaParser;
    ReadOnlySpan<char> _headBeforeTrivia;
    ImmutableArray<Trivia> _leadingTrivias;
    EndOfInputToken? _endOfInput;
    ReadOnlySpan<char> _lowLevelTokenText;
    NodeType _lowLevelTokenType;
    int _lastSuccessfulHead;

    /// <summary>
    /// Initializes a new head on a text.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="behavior">Required <see cref="IParserHeadBehavior"/>.</param>
    /// <param name="triviaBuilder">Trivia builder to use.</param>
    public ParserHead( ReadOnlyMemory<char> text,
                       IParserHeadBehavior behavior,
                       ImmutableArray<Trivia>.Builder? triviaBuilder = null )
    {
        _memText = text;
        _text = _memText.Span;
        _head = _text;
        _triviaBuilder = triviaBuilder ?? ImmutableArray.CreateBuilder<Trivia>();
        _behavior = behavior;
        _triviaParser = behavior.ParseTrivia;
        InitializeLeadingTrivia();
    }

    /// <summary>
    /// Creates an independent head on the <see cref="RemainingText"/> that can use an alternative <see cref="IParserHeadBehavior"/>.
    /// <para>
    /// <see cref="SkipTo(ref readonly ParserHead)"/> can be used to resynchronize this head with the subordinated one.
    /// </para>
    /// </summary>
    /// <param name="behavior">Alternative behavior for this new head. When null, the same behavior as this one is used.</param>
    public readonly ParserHead CreateSubHead( out int safetyToken, IParserHeadBehavior? behavior = null )
    {
        safetyToken = _lastSuccessfulHead;
        return new ParserHead( RemainingText, behavior ?? _behavior, _triviaBuilder );
    }

    public void SkipTo( int safetyToken, ref readonly ParserHead subHead )
    {
        Throw.CheckArgument( "The SubHead has not been created from this head.", _headBeforeTrivia.Overlaps( subHead.Text.Span ) );
        Throw.CheckState( _lastSuccessfulHead == safetyToken );
        _head = _headBeforeTrivia.Slice( subHead._lastSuccessfulHead );
        InitializeLeadingTrivia();
    }


    /// <summary>
    /// Gets the current head to analyze.
    /// </summary>
    public readonly ReadOnlySpan<char> Head => _head;

    /// <summary>
    /// Gets the whole text (the origin of the head).
    /// </summary>
    public readonly ReadOnlyMemory<char> Text => _memText;

    /// <summary>
    /// Gets the remaining text (the <see cref="Text"/> after the last successful token read).
    /// <para>
    /// The <see cref="Head"/> is positioned after the leading trivias of the future token.
    /// </para>
    /// </summary>
    /// <returns></returns>
    public readonly ReadOnlyMemory<char> RemainingText => _memText.Slice( _lastSuccessfulHead );

    /// <summary>
    /// Gets the enf of input if it has been reached.
    /// </summary>
    public readonly EndOfInputToken? EndOfInput => _endOfInput;

    /// <summary>
    /// Gets the low-level token type.
    /// <see cref="NodeType.None"/> if it has not been computed by <see cref="IParserHeadBehavior.LowLevelTokenize(ReadOnlySpan{char})"/>.
    /// </summary>
    public readonly NodeType LowLevelTokenType => _lowLevelTokenType;

    /// <summary>
    /// Gets the <see cref="LowLevelToken"/> text.
    /// </summary>
    public readonly ReadOnlySpan<char> LowLevelTokenText => _lowLevelTokenText;

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
        Throw.CheckState( EndOfInput is null );
        Throw.DebugAssert( !_leadingTrivias.IsDefault );

        text = _memText.Slice( _text.Length - _head.Length, tokenLength );
        _head = _head.Slice( tokenLength );
        leading = _leadingTrivias;
        var c = new TriviaHead( _head, _memText, _triviaBuilder );
        c.ParseTrailingTrivias( _triviaParser );
        trailing = _triviaBuilder.DrainToImmutable();
        // Before preloading the leading trivia for the next token, save the
        // current head position. RemainingText is based on this index.
        _headBeforeTrivia = c.Head;
        _lastSuccessfulHead = _memText.Length - _head.Length + c.Length;
        c.ParseAll( _triviaParser );
        _leadingTrivias = _triviaBuilder.DrainToImmutable();
        _head = _head.Slice( c.Length );
        // Resets the current low-level token.
        _lowLevelTokenType = NodeType.None;
        _lowLevelTokenText = default;
        if( c.IsEndOfInput ) _endOfInput = new EndOfInputToken( _leadingTrivias );
        else InitializeLowLevelToken();
    }

    /// <summary>
    /// Accepts the current <see cref="Head"/> and creates a basic <see cref="TokenNode"/> of the <paramref name="type"/>
    /// and forwards the head.
    /// <para>
    /// If <see cref="EndOfInput"/> exists, it is returned instead.
    /// </para>
    /// </summary>
    /// <param name="type">The <see cref="TokenNode.NodeType"/> to create.</param>
    /// <param name="tokenLength">The length of the token. Must be positive.</param>
    /// <returns>The token node.</returns>
    public TokenNode CreateToken( NodeType type, int tokenLength )
    {
        if( EndOfInput != null ) return EndOfInput;
        Throw.CheckArgument( !type.IsError() &&  !type.IsTrivia() );
        AcceptToken( tokenLength, out var text, out var leading, out var trailing );
        // Use the internal unchecked constructor as every parameters have been checked.
        return new TokenNode( leading, trailing, type, text );
    }

    /// <summary>
    /// Accepts the <see cref="LowLevelToken"/>, creates a basic <see cref="TokenNode"/>and forwards the head.
    /// <para>
    /// If a <see cref="EndOfInput"/> exists, it is returned instead.
    /// </para>
    /// </summary>
    /// <param name="type">The token type to create. Defaults to <see cref="LowLevelTokenType"/>.</param>
    /// <returns>The token node.</returns>
    public TokenNode CreateLowLevelToken( NodeType type = NodeType.None )
    {
        Throw.CheckState( _lowLevelTokenText.Length > 0 );
        if( type == NodeType.None ) type = _lowLevelTokenType;
        return CreateToken( type, _lowLevelTokenText.Length );
    }

    /// <summary>
    /// Helper function for easy case that matches the <see cref="LowLevelTokenText"/> and
    /// creates a <see cref="TokenNode"/> on success.
    /// </summary>
    /// <param name="expectedText">The text that must match the <see cref="LowLevelTokenText"/>. Must not be empty.</param>
    /// <param name="result">The non null TokenNode on success.</param>
    /// <param name="type">The token type to create. Defaults to <see cref="LowLevelTokenType"/>.</param>
    /// <param name="comparisonType">Optional comparison type.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool TryMatchToken( ReadOnlySpan<char> expectedText,
                               [NotNullWhen( true )] out TokenNode? result,
                               NodeType type = NodeType.None,
                               StringComparison comparisonType = StringComparison.Ordinal )
    {
        Throw.CheckArgument( expectedText.Length > 0 );
        if( _lowLevelTokenText.Equals( expectedText, comparisonType ) )
        {
            if( type == NodeType.None ) type = _lowLevelTokenType;
            result = CreateToken( type, expectedText.Length );
            return true;
        }
        result = null;
        return false;
    }

    /// <summary>
    /// Creates a token error node at the current <see cref="Head"/> position.
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
    /// <returns></returns>
    public TokenNode MatchToken( ReadOnlySpan<char> expected,
                                 NodeType type = NodeType.None,
                                 StringComparison comparisonType = StringComparison.Ordinal )
    {
        return TryMatchToken( expected, out var n, type, comparisonType )
                ? n
                : CreateError( $"Expected '{expected}'." );
    }


    #region Internal & private

    void InitializeLeadingTrivia()
    {
        // Creates the Trivia head and collects every possible trivias thanks to the
        // current trivia parser.
        _headBeforeTrivia = _head;
        var c = new TriviaHead( _head, _memText, _triviaBuilder );
        c.ParseAll( _triviaParser );
        _leadingTrivias = _triviaBuilder.DrainToImmutable();
        _head = _head.Slice( c.Length );
        if( c.IsEndOfInput ) _endOfInput = new EndOfInputToken( _leadingTrivias );
        else InitializeLowLevelToken();
    }

    void InitializeLowLevelToken()
    {
        // Initializes the low-level token.
        var t = _behavior.LowLevelTokenize( _head );
        _lowLevelTokenType = t.NodeType;
        if( t.Length != 0 )
        {
            Throw.CheckState( t.Length >= 0 );
            _lowLevelTokenText = _head.Slice( 0, t.Length );
        }
    }

    readonly SourcePosition CreateSourcePosition() => SourcePosition.GetSourcePosition( _text, _text.Length - _head.Length );

    #endregion
}

