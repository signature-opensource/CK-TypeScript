using CK.Core;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// Collection of typed <see cref="IAbstractNode"/>.
/// </summary>
/// <typeparam name="T">The collection item type.</typeparam>
public class NodeList<T> : CollectionNode, IAbstractNodeList<T> where T : class, IAbstractNode
{
    /// <summary>
    /// Initializes a new list from its children without trivias.
    /// </summary>
    /// <param name="children">The children.</param>
    public NodeList( params IEnumerable<T> children )
        : base( children.Cast<AbstractNode>().ToImmutableArray() )
    {
    }

    /// <summary>
    /// Initializes a new list from its children and trivias.
    /// <para>
    /// The content should be check by calling <see cref="AbstractNode.CheckInvariants()"/>
    /// unless this is called by <see cref="CollectionNodeMutator"/> (that does this automatically).
    /// </para>
    /// </summary>
    /// <param name="leading">The leading trivias.</param>
    /// <param name="trailing">The trailing trivias.</param>
    /// <param name="uncheckedChildren">The children.</param>
    public NodeList( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, params ImmutableArray<AbstractNode> uncheckedChildren )
        : base( leading, trailing, uncheckedChildren )
    {
    }

    /// <inheritdoc />
    public T this[int index] => Unsafe.As<T>( _children[index] );

    /// <inheritdoc />
    public int Count =>_children.Length;

    /// <summary>
    /// Gives access to the internal store.
    /// </summary>
    protected ImmutableArray<AbstractNode> Children => _children;

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _children.Cast<T>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Checks that all stored items are <typeparamref name="T"/> and are not <see cref="ErrorTolerant.IErrorTolerantNode"/>.
    /// </summary>
    protected override void DoCheckInvariants()
    {
        Throw.CheckArgument( _children.All( c => c is T && c is not ErrorTolerant.IErrorTolerantNode ) );
    }

    /// <inheritdoc />
    /// <remarks>
    /// This MUST be overridden if this class is specialized.
    /// </remarks>
    protected internal override AbstractNode DoClone( ImmutableArray<Trivia> leading, CollectionNodeMutator content, ImmutableArray<Trivia> trailing )
    {
        Throw.CheckState( "DoClone() MUST be overridden.", GetType() == typeof( NodeList<T> ) );
        return new NodeList<T>( leading, trailing, content.RawItems.ToImmutableArray() );
    }
}


