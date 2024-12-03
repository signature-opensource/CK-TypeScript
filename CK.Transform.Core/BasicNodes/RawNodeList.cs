using CK.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

/// <summary>
/// Collection of any <see cref="AbstractNode"/>.
/// </summary>
public class RawNodeList : CollectionNode, IAbstractNodeList<AbstractNode>
{
    readonly ImmutableArray<AbstractNode> _children;
    readonly NodeType _nodeType;

    /// <summary>
    /// Initializes a new list from its children without trivias.
    /// </summary>
    /// <param name="nodeType">The node type.</param>
    /// <param name="children">The children.</param>
    public RawNodeList( NodeType nodeType, params ImmutableArray<AbstractNode> children )
    {
        _nodeType = nodeType;
        _children = children;
    }

    /// <summary>
    /// Initializes a new list from its children and trivias.
    /// </summary>
    /// <param name="nodeType">The node type.</param>
    /// <param name="leading">The leading trivias.</param>
    /// <param name="trailing">The trailing trivias.</param>
    /// <param name="children">The children.</param>
    public RawNodeList( NodeType nodeType, ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, ImmutableArray<AbstractNode> children )
        : base( leading, trailing )
    {
        _nodeType = nodeType;
        _children = children;
    }

    public override NodeType NodeType => _nodeType;

    /// <inheritdoc />
    public AbstractNode this[int index] => _children[index];

    /// <inheritdoc />
    public override sealed IReadOnlyList<AbstractNode> ChildrenNodes => _children;

    /// <inheritdoc />
    public int Count =>_children.Length;

    /// <inheritdoc />
    public IEnumerator<AbstractNode> GetEnumerator() => ChildrenNodes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gives access to the children store.
    /// </summary>
    protected ImmutableArray<AbstractNode> Children => _children;

    /// <summary>
    /// Checks that all stored items are not null.
    /// </summary>
    protected override void DoCheckInvariants()
    {
        Throw.CheckArgument( !NodeType.IsError() && !NodeType.IsTrivia() );
        Throw.CheckArgument( Children.All( c => c != null ) );
    }

    /// <inheritdoc />
    /// <remarks>
    /// This MUST be overridden if this class is specialized.
    /// </remarks>
    protected internal override AbstractNode DoClone( ImmutableArray<Trivia> leading, CollectionNodeMutator content, ImmutableArray<Trivia> trailing )
    {
        Throw.CheckState( "DoClone() MUST be overridden.", GetType() == typeof( RawNodeList ) );
        return new RawNodeList( _nodeType, leading, trailing, content.RawItems.ToImmutableArray() );
    }
}


