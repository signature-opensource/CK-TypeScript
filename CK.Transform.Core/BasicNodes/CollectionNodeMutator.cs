using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CK.Transform.Core;

public class CollectionNodeMutator : AbstractNodeMutator
{
    List<AbstractNode>? _items;

    /// <summary>
    /// Initializes a mutator for a <see cref="CollectionNode"/>.
    /// <para>
    /// When specialized, the constructor must not be public. It should always be internal (or
    /// internal protected if the mutator is not sealed) so that it can only be a obtain by a call to
    /// the origin node's <see cref="AbstractNode.CreateMutator()"/> method.
    /// </para>
    /// </summary>
    /// <param name="node">The node to mutate.</param>
    internal protected CollectionNodeMutator( CollectionNode node )
        : base( node )
    {
    }

    /// <inheritdoc cref="AbstractNodeMutator.Origin" />
    public new CollectionNode Origin => Unsafe.As<CollectionNode>( base.Origin );

    /// <summary>
    /// Gets the fully mutable items list.
    /// </summary>
    public IList<AbstractNode> RawItems => _items ??= new List<AbstractNode>( Origin.ChildrenNodes );

    /// <summary>
    /// Creates the mutated node from this <see cref="RawItems"/> and trivias (or returns the <see cref="Origin"/>
    /// when nothing changed).
    /// <para>
    /// This method can return a different node type than the <see cref="Origin"/> one. This allows mutations to be handled
    /// at the node level, not only at the parent/child seam.
    /// </para>
    /// </summary>
    /// <returns>The cloned node.</returns>
    public override sealed AbstractNode Clone()
    {
        CollectionNodeMutator? content = _items != null && !_items.SequenceEqual( Origin.ChildrenNodes )
                                            ? this
                                            : null;
        var triviaChanged = GetTrivias( out var leading, out var trailing );
        return content != null
                ? CreateAndCheck( Origin, leading, content, trailing )
                : triviaChanged
                    ? Origin.CloneForTrivias( leading, trailing )
                    : Origin;

        static AbstractNode CreateAndCheck( CollectionNode origin, ImmutableArray<Trivia> leading, CollectionNodeMutator content, ImmutableArray<Trivia> trailing )
        {
            var o = origin.DoClone( leading, content, trailing );
            o.CheckInvariants();
            return o;
        }
    }

    internal override sealed AbstractNode? GetFirstChildForTrivia( out int idx )
    {
        idx = 0;
        var c = Origin.ChildrenNodes;
        return c.Count > 0 ? c[0] : null;
    }

    internal override sealed AbstractNode? GetLastChildForTrivia( out int idx )
    {
        var c = Origin.ChildrenNodes;
        if( c.Count > 0 )
        {
            idx = c.Count - 1;
            return c[idx];
        }
        idx = 0;
        return null;
    }

    internal override sealed void ReplaceForTrivia( int idx, AbstractNode n )
    {
        RawItems[idx] = n;
    }
}
