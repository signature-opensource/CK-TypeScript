using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Base class for all kind of list that are any <see cref="SyntaxNode"/> with a
/// variable number of children. The other kind of SyntaxNode are <see cref="CompositeNode"/>.
/// </summary>
public abstract partial class CollectionNode : SyntaxNode
{
    internal readonly ImmutableArray<AbstractNode> _children;

    /// <summary>
    /// Initializes a new <see cref="CollectionNode"/> from its children and no trivias.
    /// </summary>
    /// <param name="uncheckedChildren">The children.</param>
    public CollectionNode( params ImmutableArray<AbstractNode> uncheckedChildren )
    {
        _children = uncheckedChildren;
    }

    /// <summary>
    /// Initializes a new <see cref="CollectionNode"/>.
    /// </summary>
    /// <param name="leading">The leading trivias.</param>
    /// <param name="trailing">The trailing trivias.</param>
    /// <param name="uncheckedChildren">The children.</param>
    protected CollectionNode( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, params ImmutableArray<AbstractNode> uncheckedChildren )
        : base( leading, trailing )
    {
        _children = uncheckedChildren;
    }

    /// <inheritdoc />
    public override sealed IReadOnlyList<AbstractNode> ChildrenNodes => _children;

    /// <inheritdoc />
    public override CollectionNodeMutator CreateMutator() => new CollectionNodeMutator( this );

    /// <summary>
    /// Fundamental method that rebuilds this Node with a mutated content.
    /// The <paramref name="content"/> must be checked in depth and must throw on any incoherency.
    /// <para>
    /// This is called only if a mutation is required because the content has changed (Trivias mutations
    /// are handled independently).
    /// </para>
    /// <para>
    /// This method is allowed to return a different node type than this one. This allows mutations to be handled
    /// at the node level, not only at the parent/child seam.
    /// </para>
    /// </summary>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="content">New content to handle.</param>
    /// <param name="trailing">Trailing trivias.</param>
    /// <returns>A new immutable object.</returns>
    internal protected abstract AbstractNode DoClone( ImmutableArray<Trivia> leading, CollectionNodeMutator content, ImmutableArray<Trivia> trailing );
}
