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
/// An analyzer encapsulates a <see cref="NodeParser"/> in a stateful context.
/// </summary>
public abstract partial class Analyzer : IParserHeadBehavior
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
    /// Gets the not yet analyzed text.
    /// </summary>
    public ReadOnlyMemory<char> RemainingText => _head;

    /// <summary>
    /// Returns the next node.
    /// </summary>
    /// <returns>The next node.</returns>
    public IAbstractNode? Parse()
    {
        var head = new ParserHead( _head, this, _triviaBuilder );
        var n = Parse( ref head );
        Throw.DebugAssert( "IAbstractNode is necessarily a AnstractNode.", n is null || n is AbstractNode );
        _head = head.GetRemainingText();
        return n;
    }

    /// <summary>
    /// Gets the whole text.
    /// </summary>
    public ReadOnlyMemory<char> Text => _text;

    /// <summary>
    /// Tries to read a top-level language node from the <see cref="ParserHead"/>.
    /// <para>
    /// When this analyzer doesn't recognize the language (typically because the very first token is unknwon), it must return null.
    /// </para>
    /// <para>
    /// The notion of "top-level" is totally language dependent. A language can perfectly decide that a list of statements must be handled
    /// as a top-level node.
    /// </para>
    /// </summary>
    /// <param name="head">The <see cref="ParserHead"/>.</param>
    /// <returns>The node (can be a <see cref="TokenErrorNode"/>).</returns>
    protected abstract IAbstractNode? Parse( ref ParserHead head );

    /// <inheritdoc />
    public abstract void ParseTrivia( ref TriviaHead c );

    /// <inheritdoc />
    public abstract LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head );

}
