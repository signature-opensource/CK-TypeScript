using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.ErrorTolerant;

public sealed class SyntaxErrorNode : NodeList<AbstractNode>, IErrorTolerantNode
{
    /// <summary>
    /// Initializes a new <see cref="SyntaxErrorNode"/> from its children and trivias.
    /// <para>
    /// At least one child must be a <see cref="IErrorTolerantNode"/>.
    /// </para>
    /// </summary>
    /// <param name="leading">The leading trivias.</param>
    /// <param name="trailing">The trailing trivias.</param>
    /// <param name="children">The children.</param>
    public SyntaxErrorNode( ImmutableArray<Trivia> leading,
                            ImmutableArray<Trivia> trailing,
                            params ImmutableArray<AbstractNode> children )
        : base( leading, trailing, children )
    {
        DoCheckInvariants();
    }

    /// <summary>
    /// Overridden to skip the check as children are de facto <see cref="AbstractNode"/>
    /// and this obviously skips the check that children must not be <see cref="IErrorTolerantNode"/>.
    /// <para>
    /// A contrario, this chacks that at least one child is a <see cref="IErrorTolerantNode"/>.
    /// </para>
    /// </summary>
    protected override void DoCheckInvariants()
    {
        Throw.CheckArgument( Children.Any( c => c is IErrorTolerantNode ) );
    }

    /// <summary>
    /// Required override.
    /// </summary>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="content">New content to handle.</param>
    /// <param name="trailing">Trailing trivias.</param>
    /// <returns>A new immutable object.</returns>
    protected internal override AbstractNode DoClone( ImmutableArray<Trivia> leading, CollectionNodeMutator content, ImmutableArray<Trivia> trailing )
    {
        return new SyntaxErrorNode( leading, trailing, content.RawItems.ToImmutableArray() );
    }
}
