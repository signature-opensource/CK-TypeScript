using CK.Transform.Core;
using System.Collections.Immutable;

namespace CK.Html.Transform;

public class Class1 : IAbstractNode
{
    public NodeType NodeType => throw new NotImplementedException();

    public IReadOnlyList<AbstractNode> ChildrenNodes => throw new NotImplementedException();

    public IEnumerable<AbstractNode> TrailingNodes => throw new NotImplementedException();

    public IEnumerable<AbstractNode> LeadingNodes => throw new NotImplementedException();

    public ImmutableArray<Trivia> LeadingTrivias => throw new NotImplementedException();

    public IEnumerable<Trivia> FullLeadingTrivias => throw new NotImplementedException();

    public ImmutableArray<Trivia> TrailingTrivias => throw new NotImplementedException();

    public IEnumerable<Trivia> FullTrailingTrivias => throw new NotImplementedException();

    public IEnumerable<TokenNode> AllTokens => throw new NotImplementedException();

    public int Width => throw new NotImplementedException();
}
