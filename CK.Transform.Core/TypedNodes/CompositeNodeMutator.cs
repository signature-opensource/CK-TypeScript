using CK.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Composite mutator.
/// </summary>
public class CompositeNodeMutator : AbstractNodeMutator
{
    AbstractNode?[]? _items;

    /// <summary>
    /// Initializes a mutator for a <see cref="CompositeNode"/>.
    /// At this level, only Trivias can be mutated.
    /// <para>
    /// When specialized, the constructor must not be public. It should always be internal (or
    /// internal protected if the mutator is not sealed) so that it can only be a obtain by a call to
    /// the origin node's <see cref="AbstractNode.CreateMutator()"/> method.
    /// </para>
    /// </summary>
    /// <param name="node">The node to mutate.</param>
    internal protected CompositeNodeMutator( CompositeNode node )
        : base( node )
    {
    }

    /// <inheritdoc cref="AbstractNodeMutator.Origin" />
    public new CompositeNode Origin => Unsafe.As<CompositeNode>( base.Origin );

    /// <summary>
    /// Gets the fully mutable children store.
    /// </summary>
    public AbstractNode?[] RawItems => _items ??= Unsafe.As<AbstractNode?[]>( Origin._store.Clone() );

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
        var content = _items != null && !_items.AsSpan().SequenceEqual( Origin._store.AsSpan() )
                        ? this
                        : null;
        var triviaChanged = GetTrivias( out var leading, out var trailing );
        return content != null
                ? CreateAndCheck( Origin, leading, content, trailing )
                : triviaChanged
                    ? Origin.CloneForTrivias( leading, trailing )
                    : Origin;

        static AbstractNode CreateAndCheck( CompositeNode origin, ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
        {
            var o = origin.DoClone( leading, content, trailing );
            o.CheckInvariants();
            return o;
        }
    }

    internal override AbstractNode? GetFirstChildForTrivia( out int idx )
    {
        Throw.DebugAssert( "Called first.", _items == null );
        for( int i = 0; i < Origin._store.Length; i++ )
        {
            var o = Origin._store[i];
            if( o != null )
            {
                idx = i;
                return o;
            }
        }
        idx = 0;
        return null;
    }

    internal override AbstractNode? GetLastChildForTrivia( out int idx )
    {
        for( int i = 0; i < Origin._store.Length; i++ )
        {
            var o = Origin._store[i];
            if( o != null )
            {
                idx = i;
                return o;
            }
        }
        idx = 0;
        return null;
    }

    internal override void ReplaceForTrivia( int idx, AbstractNode n )
    {
        RawItems[idx] = n;
    }

}
