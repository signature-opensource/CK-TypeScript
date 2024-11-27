using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace CK.Transform.Core;

/// <summary>
/// Base class for all kind of list that are any <see cref="SyntaxNode"/> with a
/// variable number of children. The other kind of SyntaxNode are <see cref="CompositeNode"/>.
/// </summary>
public abstract partial class CollectionNode : SyntaxNode
{
    /// <summary>
    /// Initializes a new <see cref="CollectionNode"/>.
    /// </summary>
    /// <param name="leading">The leading trivias.</param>
    /// <param name="trailing">The trailing trivias.</param>
    protected CollectionNode( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing )
        : base( leading, trailing )
    {
    }

    /// <summary>
    /// Initializes a new <see cref="CollectionNode"/> with no trivias.
    /// </summary>
    protected CollectionNode()
    {
    }

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
