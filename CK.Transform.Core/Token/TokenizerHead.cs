using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CK.Transform.Core;

/// <summary>
/// Tokenizer head.
/// </summary>
[DebuggerDisplay("{_head}")]
public ref struct TokenizerHead
{
    ReadOnlySpan<char> _head;
    readonly ReadOnlySpan<char> _text;
    readonly ReadOnlyMemory<char> _memText;
    readonly ImmutableArray<Trivia>.Builder _triviaBuilder;
    readonly ILowLevelTokenizer _lowLevelTokenizer;
    readonly SourceSpanRoot _spans;
    readonly List<Token> _tokens;
    Token? _lastToken;
    TriviaParser _triviaParser;
    ReadOnlySpan<char> _headBeforeTrivia;
    ImmutableArray<Trivia> _leadingTrivias;
    TokenError? _endOfInput;
    TokenError? _firstError;
    ReadOnlySpan<char> _lowLevelTokenText;
    TokenType _lowLevelTokenType;
    int _lastSuccessfulHead;
    int _inlineErrorCount;
    int _tokenCountOffset;

    /// <summary>
    /// Initializes a new head on a text.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="behavior">Required <see cref="ITokenizerHeadBehavior"/>.</param>
    /// <param name="tokenCollector">Optional preallocated token collector to use.</param>
    /// <param name="triviaBuilder">Optional preallocated trivia builder to use.</param>
    public TokenizerHead( ReadOnlyMemory<char> text,
                          ITokenizerHeadBehavior behavior,
                          List<Token>? tokenCollector = null,
                          ImmutableArray<Trivia>.Builder? triviaBuilder = null )
    {
        Throw.CheckNotNullArgument( behavior );
        _lowLevelTokenizer = behavior;
        _triviaParser = behavior.ParseTrivia;

        _memText = text;
        _text = _memText.Span;
        _head = _text;
        _tokens = tokenCollector ?? new List<Token>();
        _triviaBuilder = triviaBuilder ?? ImmutableArray.CreateBuilder<Trivia>();
        _spans = new SourceSpanRoot();
        _tokenCountOffset = -1;
        InitializeLeadingTrivia();
    }

    // Private constructor for CreateSubHead.
    TokenizerHead( ReadOnlyMemory<char> text,
                   TriviaParser triviaParser,
                   ILowLevelTokenizer lowLevelTokenizer,
                   ImmutableArray<Trivia>.Builder triviaBuilder,
                   int tokenCountOffset )
    {
        Throw.CheckNotNullArgument( triviaParser );
        Throw.CheckNotNullArgument( lowLevelTokenizer );
        _lowLevelTokenizer = lowLevelTokenizer;
        _triviaParser = triviaParser;

        _memText = text;
        _text = _memText.Span;
        _head = _text;
        _triviaBuilder = triviaBuilder;
        _spans = new SourceSpanRoot();
        _tokens = new List<Token>();
        _tokenCountOffset = tokenCountOffset;
        InitializeLeadingTrivia();
    }

    /// <summary>
    /// Creates an independent head on the <see cref="RemainingText"/> that can use an alternative <see cref="IParserHeadBehavior"/>:
    /// both trivia handling and low level tokenizer are different.
    /// <para>
    /// <see cref="SkipTo(int,ref readonly TokenizerHead)"/> can be used to resynchronize this head with the subordinated one.
    /// </para>
    /// </summary>
    /// <param name="safetyToken">Opaque token that secures the position of this head: SkipTo requires it.</param>
    /// <param name="behavior">Alternative behavior for this new head.</param>
    public readonly TokenizerHead CreateSubHead( out int safetyToken, ITokenizerHeadBehavior behavior )
    {
        safetyToken = _lastSuccessfulHead;
        return new TokenizerHead( RemainingText, behavior.ParseTrivia, behavior, _triviaBuilder, LastTokenIndex );
    }

    /// <summary>
    /// Creates an independent head on the <see cref="RemainingText"/> that can use an alternative <see cref="ILowLevelTokenizer"/>.
    /// <para>
    /// <see cref="SkipTo(int,ref readonly TokenizerHead)"/> can be used to resynchronize this head with the subordinated one.
    /// </para>
    /// </summary>
    /// <param name="safetyToken">Opaque token that secures the position of this head: SkipTo requires it.</param>
    /// <param name="lowLevelTokenizer">Alternative low level tokenizer for this new head. When null, the same low level tokenizer as this one is used.</param>
    public readonly TokenizerHead CreateSubHead( out int safetyToken, ILowLevelTokenizer? lowLevelTokenizer = null )
    {
        safetyToken = _lastSuccessfulHead;
        return new TokenizerHead( RemainingText, _triviaParser, lowLevelTokenizer ?? _lowLevelTokenizer, _triviaBuilder, LastTokenIndex );
    }

    /// <summary>
    /// Skips this head up to the <paramref name="subHead"/>.
    /// </summary>
    /// <param name="safetyToken">Token provided by CreateSubHead methods.</param>
    /// <param name="subHead">Subordinated head.</param>
    public void SkipTo( int safetyToken, ref TokenizerHead subHead )
    {
        Throw.CheckArgument( "The SubHead has not been created from this head.", _headBeforeTrivia.Overlaps( subHead.Text.Span ) );
        Throw.CheckState( _lastSuccessfulHead == safetyToken );
        _head = _headBeforeTrivia.Slice( subHead._lastSuccessfulHead );
        _tokens.AddRange( CollectionsMarshal.AsSpan( subHead._tokens ) );
        _lastToken = subHead._lastToken;
        _firstError ??= subHead._firstError;
        _inlineErrorCount += subHead._inlineErrorCount;

        subHead._tokens.Clear();
        subHead._lastToken = null;
        if( subHead._spans._children.HasChildren ) subHead._spans.TransferTo( _spans );
        subHead._firstError = null;
        subHead._inlineErrorCount = 0;

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
    /// Gets the tokens accepted so far by this head: if this is a subordinated head, this doesn't contain
    /// the tokens from the parent head.
    /// </summary>
    public readonly IReadOnlyList<Token> Tokens => _tokens;

    /// <summary>
    /// Gets the spans to which any span can be added.
    /// If this is a subordinated head, this doesn't contain the spans from the parent head.
    /// </summary>
    public readonly SourceSpanRoot Spans => _spans;

    /// <summary>
    /// Gets the last accepted token.
    /// </summary>
    public readonly Token? LastToken => _lastToken;

    /// <summary>
    /// Gets the last token index, accounting parent's tokens if this is a subordinated head.
    /// <para>
    /// This is -1 when no token have been accepted so far.
    /// </para>
    /// </summary>
    public readonly int LastTokenIndex => _tokenCountOffset + _tokens.Count;

    /// <summary>
    /// Incremented each time <see cref="AppendError(string, int, TokenType)"/> is called.
    /// </summary>
    public readonly int InlineErrorCount => _inlineErrorCount;

    /// <summary>
    /// Gets the remaining text (the <see cref="Text"/> after the last successful token read).
    /// <para>
    /// The <see cref="Head"/> is positioned after the leading trivias of the future token.
    /// </para>
    /// </summary>
    /// <returns></returns>
    public readonly ReadOnlyMemory<char> RemainingText => _memText.Slice( _lastSuccessfulHead );

    /// <summary>
    /// Gets the end of input if it has been reached.
    /// </summary>
    public readonly TokenError? EndOfInput => _endOfInput;

    /// <summary>
    /// Gets the first error that has been appended.
    /// </summary>
    public readonly TokenError? FirstError => _firstError;

    /// <summary>
    /// Gets the low-level token type.
    /// <see cref="TokenType.None"/> if it has not been computed by <see cref="IParserHeadBehavior.LowLevelTokenize(ReadOnlySpan{char})"/>.
    /// </summary>
    public readonly TokenType LowLevelTokenType => _lowLevelTokenType;

    /// <summary>
    /// Gets the <see cref="LowLevelToken"/> text.
    /// </summary>
    public readonly ReadOnlySpan<char> LowLevelTokenText => _lowLevelTokenText;

    /// <summary>
    /// Extracts the tokens accepted so far, the spans created and the <see cref="InlineErrorCount"/>.
    /// <see cref="Tokens"/> and <see cref="Spans"/> are emptied, InlineErrorCount is set to 0.
    /// </summary>
    /// <param name="code">The collected tokens and spans.</param>
    /// <param name="inlineErrorCount">Number of inlined errors.</param>
    public void ExtractResult( out SourceCode code, out int inlineErrorCount )
    {
        if( MemoryMarshal.TryGetString( _memText, out var sourceText, out var start, out var length ) )
        {
            // This will always be more efficient than rewriting the string.
            if( length < sourceText.Length ) sourceText = sourceText.Substring( start, length );
        }
        code = new SourceCode( _tokens, _spans, sourceText );
        Throw.DebugAssert( _tokens.Count == 0 && !_spans._children.HasChildren );
        inlineErrorCount = _inlineErrorCount;
        _inlineErrorCount = 0;
    }

    /// <summary>
    /// Creates an error token at the current <see cref="Head"/> position with an error message that is not added to the <see cref="Tokens"/>.
    /// This independent error must be returned by tokenizer functions and stops the process. It eventually is <see cref="AnalyzerResult.HardError"/>.
    /// </summary>
    /// <param name="errorMessage">The error message. Must not be empty.</param>
    /// <param name="errorType">The error token type.</param>
    /// <returns>An error token.</returns>
    public readonly TokenError CreateHardError( string errorMessage, TokenType errorType = TokenType.GenericError )
    {
        Throw.CheckArgument( errorType.IsError() );
        Throw.CheckArgument( !string.IsNullOrWhiteSpace( errorMessage ) );
        var p = SourcePosition.GetSourcePosition( _text, _text.Length - _head.Length );
        return new TokenError( errorType, default, errorMessage, p, _leadingTrivias, ImmutableArray<Trivia>.Empty );
    }

    /// <summary>
    /// Validates the <see cref="Head"/> first <paramref name="tokenLength"/> characters. A <see cref="Token"/> MUST be
    /// created with the final <paramref name="text"/>, <paramref name="leading"/> and <paramref name="trailing"/> data
    /// and <see cref="Accept{T}(T)"/> MUST be called.
    /// <para>
    /// <see cref="EndOfInput"/> must be null otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="tokenLength">The length of the token. Must be positive.</param>
    /// <param name="text">The resulting <see cref="TokenNode.Text"/>.</param>
    /// <param name="leading">The resulting <see cref="AbstractNode.LeadingNodes"/>.</param>
    /// <param name="trailing">The resulting <see cref="AbstractNode.TrailingNodes"/>.</param>
    public void PreAcceptToken( int tokenLength,
                                out ReadOnlyMemory<char> text,
                                out ImmutableArray<Trivia> leading,
                                out ImmutableArray<Trivia> trailing )
    {
        Throw.CheckArgument( tokenLength > 0 );
        Throw.CheckState( EndOfInput is null );
        Throw.DebugAssert( !_leadingTrivias.IsDefault );

        text = _memText.Slice( _text.Length - _head.Length, tokenLength );
        _head = _head.Slice( tokenLength );
        // Trivia handling.
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
        _lowLevelTokenType = TokenType.EndOfInput;
        _lowLevelTokenText = default;
        if( c.IsEndOfInput ) SetEndOfInput();
        else InitializeLowLevelToken();
    }

    /// <summary>
    /// Creates and accepts a token error node at the current <see cref="Head"/> with an optional length of text.
    /// <para>
    /// When <paramref name="length"/> is positive, the returned error token has the leading and trailing trivias
    /// and the head is forwarded.
    /// When length is 0, the error has the current leading trivias (and no trailing trivias), the next token will
    /// have no leading trivias.
    /// </para>
    /// <para>
    /// <see cref="EndOfInput"/> must be null otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="errorMessage">The error message. When not specified, the message is the text content.</param>
    /// <param name="length">The length of the token that is "covered" with the error.</param>
    /// <param name="errorType">The error token type.</param>
    /// <returns>An error token.</returns>
    [MemberNotNull( nameof( LastToken ) )]
    public TokenError AppendError( string errorMessage, int length = 0, TokenType errorType = TokenType.GenericError )
    {
        Throw.CheckArgument( errorType.IsError() );
        Throw.CheckState( EndOfInput is null );

        ReadOnlyMemory<char> text;
        ImmutableArray<Trivia> leading;
        ImmutableArray<Trivia> trailing;
        SourcePosition p = SourcePosition.GetSourcePosition( _text, _text.Length - _head.Length );
        if( length > 0 )
        {
            PreAcceptToken( length, out text, out leading, out trailing );
        }
        else
        {
            text = default;
            leading = _leadingTrivias;
            _leadingTrivias = ImmutableArray<Trivia>.Empty;
            trailing = ImmutableArray<Trivia>.Empty;
        }
        if( string.IsNullOrWhiteSpace( errorMessage ) ) errorMessage = text.ToString();
        var t = new TokenError( errorType, text, errorMessage, p, leading, trailing );
        _tokens.Add( t );
        _lastToken = t;
        _firstError ??= t;
        _inlineErrorCount++;
        Throw.DebugAssert( LastToken != null );
        return t;
    }

    /// <summary>
    /// Accepts the <see cref="Head"/> first <paramref name="tokenLength"/> characters, calls a token factory and accepts it.
    /// <para>
    /// <see cref="EndOfInput"/> must be null otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// <para>
    /// This often supposes a closure. To avoid such closure <see cref="PreAcceptToken(int, out ReadOnlyMemory{char}, out ImmutableArray{Trivia}, out ImmutableArray{Trivia})"/>
    /// and <see cref="Accept(Token)"/> can be used.
    /// </para>
    /// </summary>
    /// <param name="tokenLength">The length of the token. Must be positive.</param>
    /// <param name="tokenFactory">The token factory.</param>
    /// <returns>The created and accepted token.</returns>
    [MemberNotNull( nameof( LastToken ) )]
    public Token AcceptToken( int tokenLength,
                              Func<ImmutableArray<Trivia>, ReadOnlyMemory<char>, ImmutableArray<Trivia>, Token> tokenFactory )
    {
        PreAcceptToken( tokenLength, out var text, out var leading, out var trailing );
        var t = tokenFactory( leading, text, trailing );
        _tokens.Add( t );
        _lastToken = t;
        if( t is TokenError error )
        {
            _firstError ??= error;
            ++_inlineErrorCount;
        }
        Throw.DebugAssert( LastToken != null );
        return t;
    }

    /// <summary>
    /// Accepts a token externally created after a call to <see cref="PreAcceptToken(int, out ReadOnlyMemory{char}, out ImmutableArray{Trivia}, out ImmutableArray{Trivia})"/>.
    /// </summary>
    /// <param name="token">The token. This MUST be based on the PreAccept result.</param>
    /// <returns>The <paramref name="token"/>.</returns>
    [MemberNotNull( nameof( LastToken ), nameof( _lastToken ) )]
    public T Accept<T>( T token ) where T : Token
    {
        Throw.CheckNotNullArgument( token );
        Throw.CheckState( LastToken != token );
        _tokens.Add( token );
        _lastToken = token;
        Throw.DebugAssert( LastToken != null );
        if( token is TokenError error )
        {
            _firstError ??= error;
            ++_inlineErrorCount;
        }
        return token;
    }

    /// <summary>
    /// Accepts the current <see cref="Head"/> with a positive <paramref name="tokenLength"/>, creates 
    /// a <see cref="Token"/> and forwards the head.
    /// <para>
    /// <see cref="EndOfInput"/> must be null otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="type">The <see cref="Token.TokenType"/> to create.</param>
    /// <param name="tokenLength">The length of the token. Must be positive.</param>
    /// <returns>The token.</returns>
    [MemberNotNull( nameof( LastToken ), nameof( _lastToken ) )]
    public Token AcceptToken( TokenType type, int tokenLength )
    {
        Throw.CheckArgument( !type.IsError() && !type.IsTrivia() );
        PreAcceptToken( tokenLength, out var text, out var leading, out var trailing );
        // Use the internal unchecked constructor as every parameters have been checked.
        var t = new Token( type, leading, text, trailing );
        _tokens.Add( t );
        _lastToken = t;
        Throw.DebugAssert( LastToken != null );
        return t;
    }

    /// <summary>
    /// Accepts the <see cref="LowLevelTokenText"/> and appends a <see cref="Token"/> with it.
    /// <para>
    /// <see cref="EndOfInput"/> must be null otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </para>
    /// </summary>
    /// <param name="type">The token type to create. Defaults to <see cref="LowLevelTokenType"/>.</param>
    /// <returns>The token.</returns>
    [MemberNotNull( nameof( LastToken ) )]
    public Token AcceptLowLevelToken( TokenType type = TokenType.None )
    {
        Throw.CheckState( _lowLevelTokenText.Length > 0 );
        if( type == TokenType.None ) type = _lowLevelTokenType;
        return AcceptToken( type, _lowLevelTokenText.Length );
    }

    /// <summary>
    /// Helper function for easy case that matches the <see cref="LowLevelTokenText"/> and
    /// accepts the low level token on success.
    /// </summary>
    /// <param name="expectedText">The text that must match the <see cref="LowLevelTokenText"/>. Must not be empty.</param>
    /// <param name="result">The non null TokenNode on success.</param>
    /// <param name="type">The token type to create. Defaults to <see cref="LowLevelTokenType"/>.</param>
    /// <param name="comparisonType">Optional comparison type.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool TryAcceptToken( ReadOnlySpan<char> expectedText,
                                [NotNullWhen( true )] out Token? result,
                                TokenType type = TokenType.None,
                                StringComparison comparisonType = StringComparison.Ordinal )
    {
        Throw.CheckArgument( expectedText.Length > 0 );
        if( _lowLevelTokenText.Equals( expectedText, comparisonType ) )
        {
            if( type == TokenType.None ) type = _lowLevelTokenType;
            result = AcceptToken( type, expectedText.Length );
            return true;
        }
        result = null;
        return false;
    }


    /// <summary>
    /// Helper function for easy case that matches the <see cref="LowLevelTokenType"/> and
    /// accepts the low level token on success.
    /// </summary>
    /// <param name="type">The expected <see cref="LowLevelTokenType"/>.</param>
    /// <param name="result">The non null TokenNode on success.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool TryAcceptToken( TokenType type, [NotNullWhen( true )] out Token? result )
    {
        Throw.DebugAssert( type != TokenType.None && type.IsError() is false );
        if( _lowLevelTokenType == type )
        {
            result = AcceptToken( type, _lowLevelTokenText.Length );
            return true;
        }
        result = null;
        return false;
    }

    /// <summary>
    /// Matches the <see cref="LowLevelTokenText"/> and returns a <see cref="Token"/> on success
    /// or emit a <see cref="TokenError"/> "Expected '<paramref name="expected"/>'." on failure.
    /// </summary>
    /// <param name="expected">The expected characters.</param>
    /// <param name="type">The token type to create. Defaults to <see cref="LowLevelToken.TokenType"/>.</param>
    /// <param name="errorType">By default, the error type is the expected <paramref name="type"/> | <see cref="TokenType.ErrorClassBit"/>.</param>
    /// <returns>The Token or <see cref="TokenError"/>.</returns>
    public Token MatchToken( ReadOnlySpan<char> expected,
                             TokenType type = TokenType.None,
                             StringComparison comparisonType = StringComparison.Ordinal,
                             TokenType errorType = TokenType.None )
    {
        if( TryAcceptToken( expected, out var n, type, comparisonType ) ) return n;
        if( type == TokenType.None ) type = _lowLevelTokenType;
        if( errorType == TokenType.None ) errorType = type | TokenType.ErrorClassBit;
        return AppendError( $"Expected '{expected}'.", _lowLevelTokenText.Length, errorType );
    }

    /// <summary>
    /// Appends a missing <see cref="TokenError"/> with "Missing '<paramref name="missingDescription"/>'." error message
    /// and a length of 0 (the head stays where it is).
    /// </summary>
    /// <param name="missingDescription">Describes what is missing, for example "target (string or identifier)".</param>
    /// <param name="errorType">Specific missing error type if required.</param>
    /// <returns>The inlined error.</returns>
    public TokenError AppendMissingToken( string missingDescription, TokenType errorType = TokenType.GenericMissingToken )
    {
        return AppendError( $"Missing '{missingDescription}'.", 0, errorType );
    }

    /// <summary>
    /// Appends the current <see cref="LowLevelTokenText"/> as an error.
    /// </summary>
    /// <param name="errorType">Specific missing error type if required.</param>
    /// <returns>The inlined error.</returns>
    [MemberNotNull( nameof( LastToken ) )]
    public TokenError AppendUnexpectedToken( TokenType errorType = TokenType.GenericUnexpectedToken )
    {
        return AppendError( $"Unexpected token.", _lowLevelTokenText.Length, errorType );
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
        if( c.IsEndOfInput ) SetEndOfInput();
        else InitializeLowLevelToken();
    }

    void InitializeLowLevelToken()
    {
        // Initializes the low-level token.
        var t = _lowLevelTokenizer.LowLevelTokenize( _head );
        _lowLevelTokenType = t.TokenType;
        if( t.Length != 0 )
        {
            Throw.CheckState( t.Length >= 0 );
            _lowLevelTokenText = _head.Slice( 0, t.Length );
        }
    }

    void SetEndOfInput()
    {
        _endOfInput = CreateHardError( "End of input.", TokenType.EndOfInput );
    }

    #endregion
}

