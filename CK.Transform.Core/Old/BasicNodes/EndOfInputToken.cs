using System.Collections.Immutable;

namespace CK.Transform.Core;

public sealed class EndOfInputToken : TokenNode
{
    internal EndOfInputToken( ImmutableArray<Trivia> leading )
        : base( leading, ImmutableArray<Trivia>.Empty, TokenType.EndOfInput, default )
    {
    }
}
