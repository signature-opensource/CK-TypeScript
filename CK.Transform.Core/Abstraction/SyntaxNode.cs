using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Base class for all non token nodes, composed of other nodes (some of them being tokens).
/// <para>
/// The only specializations are <see cref="NodeList{T}"/> and <see cref="CompositeNode"/>.
/// </para>
/// </summary>
public abstract class SyntaxNode : AbstractNode
{
    int _width;

    /// <summary>
    /// Initializes a new <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="leading">The leading trivias.</param>
    /// <param name="trailing">The trailing trivias.</param>
    private protected SyntaxNode( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing )
        : base( leading, trailing )
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SyntaxNode"/>.
    /// </summary>
    private protected SyntaxNode()
        : base( [], [] )
    {
    }

    /// <summary>
    /// Always <see cref="TokenNode.None"/>.
    /// </summary>
    public override sealed TokenType TokenType => TokenType.None;

    /// <summary>
    /// Gets the total number of token that this element contains.
    /// </summary>
    public override sealed int Width => _width == -1 ? (_width = ChildrenNodes.Select( c => c.Width ).Sum()) : _width;

    /// <summary>
    /// Gets a list starting with this node and the first nodes recursively.
    /// </summary>
    public override sealed IEnumerable<AbstractNode> LeadingNodes
    {
        get
        {
            AbstractNode n = this;
            for(; ; )
            {
                yield return n;
                if( n.ChildrenNodes.Count == 0 ) yield break;
                n = n.ChildrenNodes[0];
            }
        }
    }

    /// <summary>
    /// Gets a list starting with this node and the last nodes recursively.
    /// </summary>
    public override sealed IEnumerable<AbstractNode> TrailingNodes
    {
        get
        {
            AbstractNode n = this;
            for(; ; )
            {
                yield return n;
                if( n.ChildrenNodes.Count == 0 ) yield break;
                n = n.ChildrenNodes[n.ChildrenNodes.Count - 1];
            }
        }
    }

    /// <summary>
    /// Gets the leading trivias of all the <see cref="LeadingNodes"/>.
    /// </summary>
    public override sealed IEnumerable<Trivia> FullLeadingTrivias => LeadingNodes.SelectMany( n => n.LeadingTrivias );

    /// <summary>
    /// Gets the trailing trivias of all the <see cref="TrailingNodes"/>.
    /// </summary>
    public override sealed IEnumerable<Trivia> FullTrailingTrivias => TrailingNodes.Reverse().SelectMany( n => n.TrailingTrivias );

    /// <summary>
    /// Enumerates all the tokens that this node contains.
    /// </summary>
    public override sealed IEnumerable<TokenNode> AllTokens => ChildrenNodes.ToTokens();

}
