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
/// Abstract analyzer: <see cref="ParseTrivia(ref TriviaHead)"/> and <see cref="Parse(ref AnalyzerHead)"/> must
/// be implemented.
/// </summary>
public abstract partial class Analyzer
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
        var n = Parse( ref head  );
        Throw.CheckState( n is AbstractNode );
        _head = head.GetRemainingText();
        return n;
    }

    /// <summary>
    /// Attempts to fully parse a source text.
    /// This returns an <see cref="IAbstractNode"/> that can be <see cref="NodeList{T}"/> of <see cref="AbstractNode"/>,
    /// a single successful top-level node or a <see cref="TokenErrorNode"/>.
    /// <para>
    /// This stops at the first <see cref="TokenErrorNode"/> (including the <see cref="TokenErrorNode.Unhandled"/>).
    /// </para>
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The result.</returns>
    public IAbstractNode ParseAll()
    {
        AbstractNode? singleResult = null;
        List<AbstractNode>? multiResult = null;
        for( ; ; )
        {
            var node = Unsafe.As<AbstractNode>( ParseOne() );
            if( !node.NodeType.IsError() )
            {
                if( singleResult == null ) singleResult = node;
                else
                {
                    multiResult ??= new List<AbstractNode>() { singleResult };
                    multiResult.Add( node );
                }
            }
            else
            {
                if( node.NodeType == NodeType.EndOfInput )
                {
                    if( multiResult != null ) return new NodeList<AbstractNode>( multiResult );
                    if( singleResult != null ) return singleResult;
                }
                return node;
            }
        }
    }


    /// <summary>
    /// Tries to read a top-level language node from the <see cref="AnalyzerHead"/>.
    /// <para>
    /// When this analyzer doesn't recognize the language (typically because the very first token is unknwon), it must return a <see cref="TokenErrorNode.Unhandled"/>.
    /// </para>
    /// <para>
    /// The notion of "top-level" is totally language dependent. A language can perfectly decide that a list of statements must be handled
    /// as a top-level node. However, when possible the standard <see cref="ParseAll"/> should be used.
    /// </para>
    /// </summary>
    /// <param name="head">The <see cref="AnalyzerHead"/>.</param>
    /// <returns>The node (can be a <see cref="TokenErrorNode"/>).</returns>
    internal protected abstract IAbstractNode Parse( ref AnalyzerHead head );

    /// <summary>
    /// The default <see cref="TriviaParser"/> to apply.
    /// </summary>
    /// <param name="c">The trivia collector.</param>
    internal protected abstract void ParseTrivia( ref TriviaHead c );

    /// <summary>
    /// The default <see cref="LowLevelTokenizer"/> to apply.
    /// </summary>
    /// <param name="head">The start of the text to categorize. Leading trivias have already been handled.</param>
    /// <param name="candidate">The candidate token detected.</param>
    internal protected abstract LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head );

}
