using CK.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// Abstract Template tokenizer.
/// <para>
/// This is a stateful base object that can encapsulate any parsing related states and must expose its own API:
/// nothing is public in this base Tokenizer.
/// </para>
/// </summary>
public abstract partial class Tokenizer : ITokenizerHeadBehavior
{
    ReadOnlyMemory<char> _text;
    internal readonly ImmutableArray<Trivia>.Builder _triviaBuilder;
    readonly ImmutableArray<Token>.Builder _tokens;

    // This cannot be defined in Trivia (TypeLoadException). To be investigated.
    internal static ImmutableArray<Trivia> OneSpace => ImmutableArray.Create( new Trivia( TokenType.Whitespace, " " ) );

    /// <summary>
    /// Initializes a new Tokenizer.
    /// </summary>
    protected Tokenizer()
    {
        _triviaBuilder = ImmutableArray.CreateBuilder<Trivia>();
        _tokens = ImmutableArray.CreateBuilder<Token>();
    }

    /// <summary>
    /// Gets the whole text.
    /// </summary>
    protected ReadOnlyMemory<char> Text => _text;

    /// <summary>
    /// Resets this analyzer with a new text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    protected virtual void Reset( ReadOnlyMemory<char> text )
    {
        _text = text;
        _triviaBuilder.Clear();
        _tokens.Clear();
    }

    /// <summary>
    /// Helper method that can be used to expose the result of the actual <see cref="Tokenize(ref TokenizerHead)"/> method.
    /// <para>
    /// This method is virtual to support specific case such as the <see cref="TransformLanguage.BaseTransformParser"/> that
    /// overrides this method to throw an <see cref="InvalidOperationException"/> because a Transform parser must only handle
    /// statements.
    /// </para>
    /// </summary>
    /// <param name="tokens">
    /// The tokens on success or if all errors have been inlined.
    /// Unavailable (<see cref="ImmutableArray{T}.IsDefault"/> is true) if <paramref name="error"/> is not null.
    /// </param>
    /// <param name="error">The error (hard failure) if any.</param>
    /// <returns>True on success, false if <paramref name="error"/> is not null or at least a <see cref="TokenError"/> appears in the <paramref name="tokens"/>.</returns>
    protected virtual bool Tokenize( out ImmutableArray<Token> tokens, out TokenError? error )
    {
        TokenizerHead head = CreateHead();
        error = Tokenize( ref head );
        if( error == null )
        {
            tokens = head.ExtractTokens( resetInlineErrorCount: false );
            return head.InlineErrorCount == 0;
        }
        tokens = default;
        return false;
    }

    /// <summary>
    /// Creates an initial head on <see cref="Text"/> with this tokenizer as the <see cref="ITokenizerHeadBehavior"/>.
    /// </summary>
    /// <returns>The tokenizer head.</returns>
    protected TokenizerHead CreateHead() => new TokenizerHead( _text, this, _tokens, _triviaBuilder );

    /// <summary>
    /// Implements the tokenization itself.
    /// </summary>
    /// <param name="head">The head to forward until an error or the end of the input.</param>
    /// <returns>An optional TokenError on failure: errors cabe accepted and collected in <see cref="TokenizerHead.Tokens"/>.</returns>
    protected abstract TokenError? Tokenize( ref TokenizerHead head );

    /// <summary>
    /// Must implement <see cref="ITokenizerHeadBehavior.ParseTrivia(ref TriviaHead)"/>.
    /// </summary>
    /// <param name="c">The trivia head.</param>
    protected abstract void ParseTrivia( ref TriviaHead c );

    void ITokenizerHeadBehavior.ParseTrivia( ref TriviaHead c ) => ParseTrivia( ref c );

    /// <summary>
    /// Must implement <see cref="ITokenizerHeadBehavior.LowLevelTokenize(ReadOnlySpan{char})"/>.
    /// </summary>
    /// <param name="c">The head.</param>
    protected abstract LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head );

    LowLevelToken ITokenizerHeadBehavior.LowLevelTokenize( ReadOnlySpan<char> head ) => LowLevelTokenize( head );
}
