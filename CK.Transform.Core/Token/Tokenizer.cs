using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Abstract Template tokenizer.
/// <para>
/// This is a stateful base object that can encapsulate any parsing related states and must expose its own API:
/// nothing is public in this base Tokenizer.
/// </para>
/// This caches a <see cref="List{T}"/> of <see cref="Token"/> and a <see cref="ImmutableArray{T}.Builder"/> of <see cref="Trivia"/>
/// that are reusable buffers for the <see cref="TokenizerHead"/>.
/// </summary>
public abstract partial class Tokenizer : ITokenizerHeadBehavior
{
    ReadOnlyMemory<char> _text;
    internal readonly ImmutableArray<Trivia>.Builder _triviaBuilder;
    bool _handleWhiteSpaceTrivias;

    // This cannot be defined in Trivia (TypeLoadException). To be investigated.
    internal static ImmutableArray<Trivia> OneSpace => ImmutableArray.Create( new Trivia( TokenType.Whitespace, " " ) );

    /// <summary>
    /// Initializes a new Tokenizer.
    /// </summary>
    /// <param name="handleWhiteSpaceTrivias">Initial <see cref="ILowLevelTokenizer.HandleWhiteSpaceTrivias"/> value.</param>
    protected Tokenizer( bool handleWhiteSpaceTrivias = true )
    {
        _triviaBuilder = ImmutableArray.CreateBuilder<Trivia>();
        _handleWhiteSpaceTrivias = handleWhiteSpaceTrivias;
    }

    bool ILowLevelTokenizer.HandleWhiteSpaceTrivias => _handleWhiteSpaceTrivias;

    /// <summary>
    /// Gets or sets whether whitespaces trivias must be handled.
    /// </summary>
    protected bool HandleWhiteSpaceTrivias
    {
        get => _handleWhiteSpaceTrivias;
        set => _handleWhiteSpaceTrivias = value;
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
    }

    /// <summary>
    /// Helper method that can be used to expose the result of the actual <see cref="Tokenize(ref TokenizerHead)"/> method as a <see cref="AnalyzerResult"/>.
    /// </summary>
    /// <returns>The analyzer result.</returns>
    protected virtual AnalyzerResult Parse()
    {
        TokenizerHead head = CreateHead();
        Tokenize( ref head );
        head.ExtractResult( out var code, out var inlineErrorCount );
        return new AnalyzerResult( code,
                                   head.FirstParseError,
                                   inlineErrorCount,
                                   head.RemainingText,
                                   head.EndOfInput );
    }

    /// <summary>
    /// Creates an initial head on <see cref="Text"/> with this tokenizer as the <see cref="ITokenizerHeadBehavior"/>.
    /// </summary>
    /// <returns>The tokenizer head.</returns>
    protected TokenizerHead CreateHead() => new TokenizerHead( _text, this, _triviaBuilder );

    /// <summary>
    /// Implements the tokenization itself.
    /// </summary>
    /// <param name="head">The head to analyse.</param>
    protected abstract void Tokenize( ref TokenizerHead head );

    /// <summary>
    /// Must implement <see cref="ITokenizerHeadBehavior.ParseTrivia(ref TriviaHead)"/>.
    /// </summary>
    /// <param name="c">The trivia head.</param>
    protected abstract void ParseTrivia( ref TriviaHead c );

    void ITokenizerHeadBehavior.ParseTrivia( ref TriviaHead c ) => ParseTrivia( ref c );

    /// <summary>
    /// Must implement <see cref="ILowLevelTokenizer.LowLevelTokenize(ReadOnlySpan{char})"/>.
    /// </summary>
    /// <param name="head">The head.</param>
    protected abstract LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head );

    LowLevelToken ILowLevelTokenizer.LowLevelTokenize( ReadOnlySpan<char> head ) => LowLevelTokenize( head );
}
