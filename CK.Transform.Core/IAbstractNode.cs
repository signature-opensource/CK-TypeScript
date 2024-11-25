using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Ultimate abstraction of a node that can be transformed.
/// <para>
/// Even if the <see cref="AbstractNode"/> class is the only implementation of this interface,
/// this interface helps defining complex composition and enables covariance support (for
/// the <see cref="IAbstractNodeList{T}"/> for instance).
/// </para>
/// <para>
/// The interface cannot be implemented in other assembly than this one: any <see cref="IAbstractNode"/>
/// is necessarily a <see cref="AbstractNode"/>.
/// </para>
/// </summary>
public interface IAbstractNode
{
    /// <summary>
    /// This can only be implemented in this assembly and <see cref="AbstractNode"/> does this
    /// with an explicit implementation to avoid poulluting its API.
    /// </summary>
    internal void ExternalImplementationsDisabled();

    /// <summary>
    /// Gets this <see cref="Core.TokenType"/>.
    /// Always <see cref="Core.TokenType.None"/> for <see cref="SyntaxNode"/>.
    /// </summary>
    TokenType TokenType { get; }

    /// <summary>
    /// Gets the direct children if any.
    /// </summary>
    IReadOnlyList<AbstractNode> ChildrenNodes { get; }

    /// <summary>
    /// Gets the trailing nodes from this one to the deepest right-most children.
    /// </summary>
    IEnumerable<AbstractNode> TrailingNodes { get; }

    /// <summary>
    /// Gets the leading nodes from this one to the deepest left-most children.
    /// </summary>
    IEnumerable<AbstractNode> LeadingNodes { get; }

    /// <summary>
    /// Leading <see cref="Trivia"/>.
    /// </summary>
    ImmutableArray<Trivia> LeadingTrivias { get; }

    /// <summary>
    /// Gets the whole leading trivias for this node and its <see cref="LeadingNodes"/>.
    /// </summary>
    IEnumerable<Trivia> FullLeadingTrivias { get; }

    /// <summary>
    /// Trailing <see cref="Trivia"/>.
    /// </summary>
    ImmutableArray<Trivia> TrailingTrivias { get; }

    /// <summary>
    /// Gets the whole trailing trivias for this node and its <see cref="TrailingNodes"/>.
    /// </summary>
    IEnumerable<Trivia> FullTrailingTrivias { get; }

    /// <summary>
    /// Gets the tokens that compose this node.
    /// </summary>
    IEnumerable<TokenNode> AllTokens { get; }

    /// <summary>
    /// Gets the number of tokens in <see cref="AllTokens"/>.
    /// </summary>
    int Width { get; }
}
