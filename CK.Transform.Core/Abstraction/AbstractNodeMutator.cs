using CK.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Mutator encapsulates clone creation for nodes.
/// <para>
/// <see cref="CompositeNode"/> has a <see cref="CompositeNodeMutator"/> and <see cref="CollectionNode"/> has
/// <see cref="CollectionNodeMutator"/>. A mutator can be specialized to its target node type to offer
/// an ad-hoc API: mutators should always be obtained through the origin node itself (<see cref="AbstractNode.CreateMutator()"/>).
/// </para>
/// </summary>
public class AbstractNodeMutator
{
    readonly AbstractNode _node;

    /// <summary>
    /// Initializes a mutator for an <see cref="AbstractNode"/>.
    /// At this level, only Trivias can be mutated.
    /// <para>
    /// When specialized, the constructor must not be public. It should always be internal (or
    /// internal protected if the mutator is not sealed) so that it can only be a obtain by a call to
    /// the origin node's <see cref="AbstractNode.CreateMutator()"/> method.
    /// </para>
    /// </summary>
    /// <param name="node">The node to mutate.</param>
    internal protected AbstractNodeMutator( AbstractNode node )
    {
        _node = node;
        Leading = node.LeadingTrivias;
        Trailing = node.TrailingTrivias;
    }

    /// <summary>
    /// Gets the immutable node.
    /// </summary>
    public AbstractNode Origin => _node;

    /// <summary>
    /// Gets or sets the leading trivias.
    /// </summary>
    public ImmutableArray<Trivia> Leading { get; set; }

    /// <summary>
    /// Gets or sets the trailing trivias.
    /// </summary>
    public ImmutableArray<Trivia> Trailing { get; set; }

    /// <summary>
    /// Gets whether they have changed and the new trivias to apply.
    /// </summary>
    /// <param name="leading">Final leading trivias.</param>
    /// <param name="trailing">Final trailing trivias.</param>
    /// <returns>True if the trivias have changed, false otherwise.</returns>
    internal bool GetTrivias( out ImmutableArray<Trivia> leading, out ImmutableArray<Trivia> trailing )
    {
        bool hasChanged = false;
        leading = Leading;
        if( leading != _node.LeadingTrivias )
        {
            if( leading.AsSpan().SequenceEqual( _node.LeadingTrivias.AsSpan() ) )
            {
                leading = _node.LeadingTrivias;
            }
            else
            {
                hasChanged |= true;
            }
        }
        trailing = Trailing;
        if( trailing != _node.TrailingTrivias )
        {
            if( trailing.AsSpan().SequenceEqual( _node.TrailingTrivias.AsSpan() ) )
            {
                trailing = _node.TrailingTrivias;
            }
            else
            {
                hasChanged |= true;
            }
        }
        return hasChanged;
    }

    /// <summary>
    /// Clones the <see cref="Origin"/> with the changed trivias or returns it.
    /// </summary>
    /// <returns>A clone of the node.</returns>
    public virtual AbstractNode Clone()
    {
        return GetTrivias( out var leading, out var trailing )
                ? _node.CloneForTrivias( leading, trailing )
                : _node;
    }

    // Support for trivias only mutation (independent of the node type: MemberwiseClone() is used).
    // Below implementations apply to TokenNode only. Composite and Collection override them.
    internal virtual AbstractNode? GetFirstChildForTrivia( out int idx )
    {
        idx = 0;
        return null;
    }

    internal virtual AbstractNode? GetLastChildForTrivia( out int idx )
    {
        idx = 0;
        return null;
    }

    internal virtual void ReplaceForTrivia( int idx, AbstractNode abstractNode )
    {
        Debug.Fail( "Never called on AbstractNodeMutator." );
    }
}
