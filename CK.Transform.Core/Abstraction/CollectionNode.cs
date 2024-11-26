using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

public abstract class CollectionNode : SyntaxNode
{
    /// <summary>
    /// Gets the store for the children nodes.
    /// </summary>
    internal protected readonly ImmutableArray<AbstractNode> Store;

    /// <summary>
    /// Initializes a new <see cref="CollectionNode"/> from its children.
    /// <para>
    /// You can use <see cref="DoCheckInvariants()"/> to validate this node if required.
    /// </para>
    /// </summary>
    /// <param name="uncheckedChildren">The node's children.</param>
    protected CollectionNode( params ImmutableArray<AbstractNode> uncheckedChildren )
    {
        Store = uncheckedChildren;
    }

    /// <summary>
    /// At this level, this only checks that the store doesn't contain any null <see cref="AbstractNode"/>
    /// but this is an invariant: the base method should always be called first at any specialization level.
    /// <para>
    /// This method is always called by <see cref="CollectionNodeMutator.Clone()"/>.
    /// </para>
    /// </summary>
    protected override void DoCheckInvariants()
    {
        Throw.CheckArgument( Store.All( c => c != null ) );
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

