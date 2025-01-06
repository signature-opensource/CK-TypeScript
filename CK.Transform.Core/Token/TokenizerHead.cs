using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CK.Transform.Core;

/// <summary>
/// Tokenizer head.
/// </summary>
public ref struct TokenizerHead
{
    ReadOnlySpan<char> _head;
    readonly ReadOnlySpan<char> _text;
    readonly ReadOnlyMemory<char> _memText;
    readonly ImmutableArray<Trivia>.Builder _triviaBuilder;
    readonly ILowLevelTokenizer _lowLevelTokenizer;
    SourceSpanRoot _spans;
    List<Token> _tokens;
    TriviaParser _triviaParser;
    ReadOnlySpan<char> _headBeforeTrivia;
    ImmutableArray<Trivia> _leadingTrivias;
    Token? _endOfInput;
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
    /// <param name="triviaBuilder">Trivia builder to use.</param>
    public TokenizerHead( ReadOnlyMemory<char> text,
                          ITokenizerHeadBehavior behavior,
                          ImmutableArray<Trivia>.Builder? triviaBuilder = null )
    {
        Throw.CheckNotNullArgument( behavior );
        _lowLevelTokenizer = behavior;
        _triviaParser = behavior.ParseTrivia;

        _memText = text;
        _text = _memText.Span;
        _head = _text;
        _triviaBuilder = triviaBuilder ?? ImmutableArray.CreateBuilder<Trivia>();
        _spans = new SourceSpanRoot();
        _tokens = new List<Token>();
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
        _inlineErrorCount += subHead._inlineErrorCount;

        _tokens.AddRange( CollectionsMarshal.AsSpan( subHead._tokens ) );
        subHead._inlineErrorCount = 0;
        subHead._tokens.Clear();

        subHead._spans.TransferTo( _spans );

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
    public readonly Token? LastToken => _tokens.Count > 0 ? _tokens[_tokens.Count-1] : null;

    /// <summary>
    /// Gets the last token index, accounting parent's tokens if this is a subordinated head.
    /// <para>
    /// This is -1 when no token have been accepted so far.
    /// </para>
    /// </summary>
    public readonly int LastTokenIndex => _tokenCountOffset + _tokens.Count;

    /// <summary>
    /// Extracts the tokens accepted so far, the spans created and the <see cref="InlineErrorCount"/>.
    /// <see cref="Tokens"/> and <see cref="Spans"/> are emptied, InlineErrorCount is set to 0.
    /// </summary>
    /// <param name="code">The collected tokens and spans.</param>
    /// <param name="inlineErrorCount">Number of inlined errors.</param>
    public void ExtractResult( out SourceCode code, out int inlineErrorCount )
    {
        code = new SourceCode( _tokens, _spans );
        inlineErrorCount = _inlineErrorCount;
        _inlineErrorCount = 0;
    }

    /// <summary>
    /// Incremented each time <see cref="CreateInlineError(string, int, TokenType)"/> is called.
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
    /// Gets the enf of input if it has been reached.
    /// </summary>
    public readonly Token? EndOfInput => _endOfInput;

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
    /// Helper that calls <see cref="CreateError(string, TokenType)"/> or <see cref="CreateInlineError(string, int, TokenType)"/>
    /// (with a 0 length).
    /// </summary>
    /// <param name="errorMessage">The error message. Must not be empty.</param>
    /// <param name="errorType">The error token type.</param>
    /// <returns>An error token.</returns>
    public TokenError CreateError( string errorMessage, bool inlineError, TokenType errorType = TokenType.GenericError )
    {
        return inlineError
                ? CreateInlineError( errorMessage, 0, errorType )
                : CreateError( errorMessage, errorType );
    }

    /// <summary>
    /// Creates an error token at the current <see cref="Head"/> position with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message. Must not be empty.</param>
    /// <param name="errorType">The error token type.</param>
    /// <returns>An error token.</returns>
    public readonly TokenError CreateError( string errorMessage, TokenType errorType = TokenType.GenericError )
    {
        Throw.CheckArgument( errorType.IsError() );
        Throw.CheckArgument( !string.IsNullOrWhiteSpace( errorMessage ) );
        var p = SourcePosition.GetSourcePosition( _text, _text.Length - _head.Length );
        return new TokenError( errorType, default, errorMessage, p, _leadingTrivias, ImmutableArray<Trivia>.Empty );
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
    public TokenError CreateInlineError( string errorMessage, int length = 0, TokenType errorType = TokenType.GenericError )
    {
        Throw.CheckArgument( errorType.IsError() );
        Throw.CheckState( EndOfInput is null );

        _inlineErrorCount++;
        ReadOnlyMemory<char> text;
        ImmutableArray<Trivia> leading;
        ImmutableArray<Trivia> trailing;
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
        var p = SourcePosition.GetSourcePosition( _text, _text.Length - _head.Length );
        var t = new TokenError( errorType, text, errorMessage, p, leading, trailing );
        _tokens.Add( t );
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
    public Token AcceptToken( int tokenLength,
                              Func<ImmutableArray<Trivia>, ReadOnlyMemory<char>, ImmutableArray<Trivia>, Token> tokenFactory )
    {
        PreAcceptToken( tokenLength, out var text, out var leading, out var trailing );
        var t = tokenFactory( leading, text, trailing );
        _tokens.Add( t );
        return t;
    }

    /// <summary>
    /// Validates the <see cref="Head"/> first <paramref name="tokenLength"/> characters. A <see cref="Token"/> MUST be
    /// created with the final <paramref name="text"/>, <paramref name="leading"/> and <paramref name="trailing"/> data
    /// and <see cref="Accept(Token)"/> MUST be called.
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
    /// Accepts a token externally created after a call to <see cref="PreAcceptToken(int, out ReadOnlyMemory{char}, out ImmutableArray{Trivia}, out ImmutableArray{Trivia})"/>.
    /// </summary>
    /// <param name="token">The token. This MUST be based on the PreAccept result.</param>
    /// <returns>The <paramref name="token"/>.</returns>
    public T Accept<T>( T token ) where T : Token
    {
        Throw.CheckNotNullArgument( token );
        Throw.CheckState( LastToken != token );
        _tokens.Add( token );
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
    public Token CreateToken( TokenType type, int tokenLength )
    {
        Throw.CheckArgument( !type.IsError() &&  !type.IsTrivia() );
        PreAcceptToken( tokenLength, out var text, out var leading, out var trailing );
        // Use the internal unchecked constructor as every parameters have been checked.
        var t = new Token( type, leading, text, trailing );
        _tokens.Add( t );
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
    public Token CreateLowLevelToken( TokenType type = TokenType.None )
    {
        Throw.CheckState( _lowLevelTokenText.Length > 0 );
        if( type == TokenType.None ) type = _lowLevelTokenType;
        return CreateToken( type, _lowLevelTokenText.Length );
    }

    /// <summary>
    /// Helper function for easy case that matches the <see cref="LowLevelTokenText"/> and
    /// creates a <see cref="Token"/> on success.
    /// </summary>
    /// <param name="expectedText">The text that must match the <see cref="LowLevelTokenText"/>. Must not be empty.</param>
    /// <param name="result">The non null TokenNode on success.</param>
    /// <param name="type">The token type to create. Defaults to <see cref="LowLevelTokenType"/>.</param>
    /// <param name="comparisonType">Optional comparison type.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool TryMatchToken( ReadOnlySpan<char> expectedText,
                               [NotNullWhen( true )] out Token? result,
                               TokenType type = TokenType.None,
                               StringComparison comparisonType = StringComparison.Ordinal )
    {
        Throw.CheckArgument( expectedText.Length > 0 );
        if( _lowLevelTokenText.Equals( expectedText, comparisonType ) )
        {
            if( type == TokenType.None ) type = _lowLevelTokenType;
            result = CreateToken( type, expectedText.Length );
            return true;
        }
        result = null;
        return false;
    }

    /// <summary>
    /// Matches the <see cref="LowLevelTokenText"/> and return a <see cref="Token"/> on success
    /// or a <see cref="TokenError"/> "Expected '<paramref name="expected"/>'." on failure.
    /// </summary>
    /// <param name="expected">The expected characters.</param>
    /// <param name="type">The token type to create. Defaults to <see cref="LowLevelToken.NodeType"/>.</param>
    /// <param name="inlineError">True to accept the error token.</param>
    /// <param name="errorType">By default, the error type is the expected <paramref name="type"/> | <see cref="TokenType.ErrorClassBit"/>.</param>
    /// <returns>The Token or <see cref="TokenError"/>.</returns>
    public Token MatchToken( ReadOnlySpan<char> expected,
                             TokenType type = TokenType.None,
                             StringComparison comparisonType = StringComparison.Ordinal,
                             bool inlineError = false,
                             TokenType errorType = TokenType.None )
    {
        if( TryMatchToken( expected, out var n, type, comparisonType ) ) return n;
        var m = $"Expected '{expected}'.";
        if( type == TokenType.None ) type = _lowLevelTokenType;
        if( errorType == TokenType.None ) errorType = type | TokenType.ErrorClassBit;
        return inlineError
                    ? CreateInlineError( m, _lowLevelTokenText.Length, errorType )
                    : CreateError( m, errorType );
    }

    /// <summary>
    /// Helper function for easy case that matches the <see cref="LowLevelTokenType"/> and
    /// creates a <see cref="TokenNode"/> on success.
    /// </summary>
    /// <param name="type">The expected <see cref="LowLevelTokenType"/>.</param>
    /// <param name="result">The non null TokenNode on success.</param>
    /// <returns>True on success, false otherwise.</returns>
    public bool TryMatchToken( TokenType type, [NotNullWhen( true )] out Token? result )
    {
        Throw.DebugAssert( type != TokenType.None && type.IsError() is false );
        if( _lowLevelTokenType == type )
        {
            result = CreateToken( type, _lowLevelTokenText.Length );
            return true;
        }
        result = null;
        return false;
    }

    /// <summary>
    /// Matches the <see cref="LowLevelTokenType"/> and return a <see cref="Token"/> on success
    /// or a <see cref="TokenError"/> with "Expected '<paramref name="tokenDescription"/>'." error message on failure.
    /// </summary>
    /// <param name="type">The expected token type.</param>
    /// <param name="tokenDescription">Description that will appear in "Expected '<paramref name="tokenDescription"/>'." error message.</param>
    /// <param name="inlineError">True to accept the error token.</param>
    /// <param name="errorType">By default, the error type is the expected <paramref name="type"/> | <see cref="TokenType.ErrorClassBit"/>.</param>
    /// <returns>The Token or <see cref="TokenError"/>.</returns>
    public Token MatchToken( TokenType type, string tokenDescription, bool inlineError = false, TokenType errorType = TokenType.None )
    {
        if( TryMatchToken( type, out var n ) ) return n;
        var m = $"Expected '{tokenDescription}'.";
        if( errorType == TokenType.None ) errorType = type | TokenType.ErrorClassBit;
        return inlineError
                    ? CreateInlineError( m, _lowLevelTokenText.Length,  errorType )
                    : CreateError( m, errorType );
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
        _lowLevelTokenType = t.NodeType;
        if( t.Length != 0 )
        {
            Throw.CheckState( t.Length >= 0 );
            _lowLevelTokenText = _head.Slice( 0, t.Length );
        }
    }

    void SetEndOfInput()
    {
        _endOfInput = new Token( TokenType.EndOfInput, _leadingTrivias, default, ImmutableArray<Trivia>.Empty );
    }

    #endregion
}

