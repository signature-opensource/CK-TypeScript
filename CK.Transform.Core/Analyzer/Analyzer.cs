using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Transform.Core;


/// <summary>
/// Abstract analyzer: <see cref="ParseTrivia(ref TriviaHead)"/> and <see cref="Parse(ref AnalyzerHead, Analyzer)"/> must
/// be implemented.
/// </summary>
public abstract partial class Analyzer : IAnalyzerBehavior
{
    ReadOnlyMemory<char> _text;
    internal readonly ImmutableArray<Trivia>.Builder _triviaBuilder;
    ReadOnlyMemory<char> _head;

    /// <summary>
    /// Initializes a new analyzer.
    /// </summary>
    protected Analyzer()
    {
        _triviaBuilder = ImmutableArray.CreateBuilder<Trivia>();
    }

    /// <summary>
    /// Resets this analyzer with a new text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    public virtual void Reset( ReadOnlyMemory<char> text )
    {
        _text = text;
        _triviaBuilder.Clear();
        _head = _text;
    }

    /// <summary>
    /// Gets the whole text.
    /// </summary>
    public ReadOnlyMemory<char> Text => _text;

    /// <summary>
    /// Gets the not yet analyzed text.
    /// </summary>
    public ReadOnlyMemory<char> RemainingText => _head;

    /// <summary>
    /// Returns the next node.
    /// </summary>
    /// <returns>The next node.</returns>
    public IAbstractNode ParseOne()
    {
        var head = new AnalyzerHead( this );
        var n = Parse( ref head );
        Throw.CheckState( n is AbstractNode );
        _head = head.GetRemainingText();
        return n;
    }

    /// <summary>
    /// Tries to read a top-level language node from the <see cref="AnalyzerHead"/>.
    /// <para>
    /// When this analyzer doesn't recognize the language (typically because the very first token is unknwon), it must return a <see cref="TokenErrorNode.Unhandled"/>.
    /// </para>
    /// <para>
    /// The notion of "top-level" is totally language dependent. A language can perfectly decide that a list of statements must be handled
    /// as a top-level node.
    /// </para>
    /// </summary>
    /// <param name="head">The <see cref="AnalyzerHead"/>.</param>
    /// <param name="newBehavior">Optional behavior that will be set after the parse.</param>
    /// <returns>The node (can be a <see cref="TokenErrorNode"/>).</returns>
    public abstract IAbstractNode Parse( ref AnalyzerHead head, IAnalyzerBehavior? newBehavior = null );

    /// <inheritdoc />
    public abstract void ParseTrivia( ref TriviaHead c );

    /// <inheritdoc />
    public abstract LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head );

}
