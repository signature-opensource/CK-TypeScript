using CK.Core;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// Collection of typed <see cref="IAbstractNode"/> separated by a <see cref="TSep"/>.
/// </summary>
/// <typeparam name="T">The collection item type.</typeparam>
public class NodeSeparatedList<T,TSep> : CollectionNode, IAbstractNodeList<T>
    where T : class, IAbstractNode
    where TSep : class, IAbstractNode
{
    readonly ImmutableArray<AbstractNode> _children;

    /// <summary>
    /// Initializes a new list from its children and trivias.
    /// <para>
    /// The content should be check by calling <see cref="AbstractNode.CheckInvariants()"/>
    /// unless this is called by <see cref="CollectionNodeMutator"/> (that does this autmoatically).
    /// </para>
    /// </summary>
    /// <param name="uncheckedChildren">The children.</param>
    public NodeSeparatedList( params ImmutableArray<AbstractNode> uncheckedChildren )
    {
        _children = uncheckedChildren;
    }

    /// <summary>
    /// Initializes a new list from its children and trivias.
    /// <para>
    /// The content should be check by calling <see cref="AbstractNode.CheckInvariants()"/>
    /// unless this is called by <see cref="CollectionNodeMutator"/> (that does this autmoatically).
    /// </para>
    /// </summary>
    /// <param name="leading">The leading trivias.</param>
    /// <param name="trailing">The trailing trivias.</param>
    /// <param name="uncheckedChildren">The children.</param>
    public NodeSeparatedList( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, params ImmutableArray<AbstractNode> uncheckedChildren )
        : base( leading, trailing )
    {
        _children = uncheckedChildren;
    }

    /// <inheritdoc />
    public T this[int index] => Unsafe.As<T>( _children[index >> 1] );

    /// <inheritdoc />
    public override sealed IReadOnlyList<AbstractNode> ChildrenNodes => _children;

    /// <inheritdoc />
    public int Count => (_children.Length + 1) >> 1;

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        for( int i = 0; i < _children.Length; i++ )
        {
            yield return Unsafe.As<T>( _children[i++] );
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Checks that children follow the pattern <typeparamref name="T"/>, <typeparamref name="TSep"/>...,<typeparamref name="T"/>.
    /// </summary>
    protected override void DoCheckInvariants()
    {
        Throw.CheckArgument( "There must be either 0 or an odd number of children.", _children.Length == 0 || (_children.Length & 1) != 0 );
        for( int i = 0; i < _children.Length; i++ )
        {
            var childrenAtIndex = _children[i];
            if( (i & 1) == 0 )
            {
                Throw.CheckArgument( childrenAtIndex is T && childrenAtIndex is not ErrorTolerant.IErrorTolerantNode );
            }
            else
            {
                Throw.CheckArgument( childrenAtIndex is TSep && childrenAtIndex is not ErrorTolerant.IErrorTolerantNode );
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// This MUST be overridden if this class is specialized.
    /// </remarks>
    protected internal override AbstractNode DoClone( ImmutableArray<Trivia> leading, CollectionNodeMutator content, ImmutableArray<Trivia> trailing )
    {
        Throw.CheckState( "DoClone() MUST be overridden.", GetType() == typeof( NodeSeparatedList<T,TSep> ) );
        return new NodeSeparatedList<T,TSep>( leading, trailing, content.RawItems.ToImmutableArray() );
    }
}


