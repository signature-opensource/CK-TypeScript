using CK.Core;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// An analyzer encapsulates a <see cref="NodeParser"/> in a stateful context.
/// </summary>
public abstract partial class Analyzer : IParserHeadBehavior
{
    ReadOnlyMemory<char> _text;
    internal readonly ImmutableArray<Trivia>.Builder _triviaBuilder;
    ReadOnlyMemory<char> _head;

    // This cannot be defined in Trivia (TypeLoadException). To be investigated.
    internal static ImmutableArray<Trivia> OneSpace => ImmutableArray.Create( new Trivia( TokenType.Whitespace, " " ) );

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
    /// Returns the next node. <see cref="RemainingText"/> is updated.
    /// </summary>
    /// <returns>The next node.</returns>
    public IAbstractNode? Parse()
    {
        var head = new ParserHead( _head, this, _triviaBuilder );
        var n = Parse( ref head );
        Throw.DebugAssert( "IAbstractNode is necessarily a AbstractNode.", n is null || n is AbstractNode );
        _head = head.RemainingText;
        return n;
    }

    /// <summary>
    /// Attempts to fully parse a source text.
    /// This returns an <see cref="IAbstractNode"/> that can be a <see cref="RawNodeList"/>,
    /// a single successful top-level node or a <see cref="TokenErrorNode"/>.
    /// <para>
    /// All the text must be successfully parsed or a <see cref="TokenErrorNode"/> is returned.
    /// This doesn't update the <see cref="RemainingText"/> since either nothing or all the <see cref="Text"/>
    /// has been analyzed.
    /// </para>
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The result.</returns>
    public IAbstractNode ParseAll()
    {
        var head = new ParserHead( _head, this, _triviaBuilder );
        AbstractNode? singleResult = null;
        ImmutableArray<AbstractNode>.Builder? multiResult = null;
        for(; ; )
        {
            var node = Unsafe.As<AbstractNode>( Parse( ref head ) );
            if( head.EndOfInput != null )
            {
                if( multiResult != null ) return new RawNodeList( TokenType.GenericAny, multiResult.DrainToImmutable() );
                if( singleResult != null ) return singleResult;
            }
            if( node == null )
            {
                return head.CreateError( "Empty or unrecognized text." );
            }
            if( node.NodeType.IsError() )
            {
                return node;
            }
            if( singleResult == null ) singleResult = node;
            else
            {
                multiResult ??= ImmutableArray.CreateBuilder<AbstractNode>();
                multiResult.Add( node );
            }
        }
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
    /// as a top-level node or that the only construct is a <see cref="RawNodeList"/> of tokens up to the end of the input.
    /// </para>
    /// </summary>
    /// <param name="head">The <see cref="ParserHead"/>.</param>
    /// <returns>The node (can be a <see cref="TokenErrorNode"/>) or null if unhandled.</returns>
    protected abstract IAbstractNode? Parse( ref ParserHead head );

    /// <inheritdoc />
    public abstract void ParseTrivia( ref TriviaHead c );

    /// <inheritdoc />
    public abstract LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head );

}
