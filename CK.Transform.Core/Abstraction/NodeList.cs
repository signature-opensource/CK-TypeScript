using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.Core;

public class NodeList : NodeList<AbstractNode>
{
    public NodeList( IEnumerable<AbstractNode> items, ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing, int minCount = 0 )
        : base( items, leading, trailing, minCount )
    {
    }

    protected override AbstractNode DoClone( ImmutableArray<Trivia> leading, IList<AbstractNode>? content, ImmutableArray<Trivia> trailing )
    {
        return base.DoClone( leading, content, trailing );
    }
}
