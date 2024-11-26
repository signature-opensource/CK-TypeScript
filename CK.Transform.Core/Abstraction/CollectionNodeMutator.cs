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
    /// At this level, only Trivias can be mutated.
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
    public IList<AbstractNode> RawItems => _items ??= new List<AbstractNode>( Origin.Store );

    /// <summary>
    /// Creates the mutated node from this <see cref="RawItems"/> and trivias (or returns the <see cref="Origin"/>
    /// when nothing changed).
    /// <para>
    /// This method can return a different node type than the <see cref="Origin"/> one. This allows mutations to be handled
    /// at the node level, not only at the parent/child seam.
    /// </para>
    /// </summary>
    /// <returns>The cloned node.</returns>
    public override AbstractNode Clone()
    {
        CollectionNodeMutator? content = _items != null && !CollectionsMarshal.AsSpan( _items ).SequenceEqual( Origin.Store.AsSpan() )
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
            o.DoCheckInvariants();
            return o;
        }
    }

    internal override AbstractNode? GetFirstChildForTrivia( out int idx )
    {
        Throw.DebugAssert( "Called first", _items == null );
        idx = 0;
        return Origin.Store.Length > 0 ? Origin.Store[0] : null;
    }

    internal override AbstractNode? GetLastChildForTrivia( out int idx )
    {
        if( Origin.Store.Length > 0 )
        {
            idx = Origin.Store.Length - 1;
            return Origin.Store[idx];
        }
        idx = 0;
        return null;
    }

    internal override void ReplaceForTrivia( int idx, AbstractNode n )
    {
        RawItems[idx] = n;
    }

}
