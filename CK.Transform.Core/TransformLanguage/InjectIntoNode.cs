using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform;

using CNode = SNode<
        TokenNode,
        ISqlHasStringValue,
        SqlTokenIdentifier,
        ISqlHasStringValue,
        SqlTokenTerminal>;

public sealed class InjectIntoNode : CompositeNode
{
    public InjectIntoNode( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing )
        : base( leading, trailing )
    {
    }

    public override IReadOnlyList<AbstractNode> ChildrenNodes => throw new NotImplementedException();

    public override IList<AbstractNode> GetRawContent()
    {
        throw new NotImplementedException();
    }

    protected override AbstractNode DoClone( ImmutableArray<Trivia> leading, IList<AbstractNode>? content, ImmutableArray<Trivia> trailing )
    {
        throw new NotImplementedException();
    }
}
