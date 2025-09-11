using CK.Core;
using System;
using System.Collections.Immutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CK.Transform.Core;

/// <summary>
/// Abstract Template Analyzer.
/// </summary>
public abstract partial class Analyzer : IAnalyzer, ITokenizerHeadBehavior
{
    internal readonly ImmutableArray<Trivia>.Builder _triviaBuilder;
    readonly bool _defaultWhiteSpaceTrivias;
    bool _handleWhiteSpaceTrivias;

    /// <summary>
    /// Initializes a new Tokenizer.
    /// </summary>
    /// <param name="handleWhiteSpaceTrivias">Initial and default <see cref="ILowLevelTokenizer.HandleWhiteSpaceTrivias"/> value.</param>
    protected Analyzer( bool handleWhiteSpaceTrivias = true )
    {
        _triviaBuilder = ImmutableArray.CreateBuilder<Trivia>();
        _defaultWhiteSpaceTrivias = handleWhiteSpaceTrivias;
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
    /// Implements <see cref="IAnalyzer.Parse(ReadOnlyMemory{char})"/>.
    /// <para>
    /// This resets any internal state before creating a <see cref="TokenizerHead"/> on the text
    /// and calling <see cref="DoParse(ref TokenizerHead)"/>.
    /// The <see cref="AnalyzerResult"/> is then built form head (see
    /// <see cref="TokenizerHead.ExtractResult(out SourceCode, out int)"/>).
    /// </para>
    /// </summary>
    /// <returns>The analyzer result.</returns>
    public virtual AnalyzerResult Parse( ReadOnlyMemory<char> text )
    {
        _handleWhiteSpaceTrivias = _defaultWhiteSpaceTrivias;
        _triviaBuilder.Clear();
        // Creates an initial head on text with this tokenizer as the ITokenizerHeadBehavior.
        TokenizerHead head = new TokenizerHead( text, this, _triviaBuilder );
        try
        {
            DoParse( ref head );
        }
        catch( Exception ex )
        {
            var p = SourcePosition.GetSourcePosition( text.Span, head.RemainingTextIndex );
            throw new CKException( $"Parse error @{p.Line},{p.Column}", ex );
        }
        head.ExtractResult( out var code, out var inlineErrorCount );
        return new AnalyzerResult( code,
                                   head.FirstError,
                                   inlineErrorCount,
                                   head.RemainingText,
                                   head.EndOfInput );
    }

    /// <summary>
    /// Implements the parsing itself.
    /// </summary>
    /// <param name="head">The head to analyse.</param>
    protected abstract void DoParse( ref TokenizerHead head );

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
