using CK.Core;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.Core;

public class TokenErrorNode : TokenNode
{
    readonly string _errorMessage;

    /// <summary>
    /// Singleton used by PartialTokenizer.
    /// </summary>
    public static readonly TokenErrorNode Unhandled = new TokenErrorNode( TokenType.ErrorUnhandled, "Unhandled" );

    public TokenErrorNode( TokenType error, string errorMessage, ImmutableArray<Trivia> leading = default, ImmutableArray<Trivia> trailing = default )
        : base( leading.IsDefault ? ImmutableArray<Trivia>.Empty : leading,
                trailing.IsDefault ? ImmutableArray<Trivia>.Empty : trailing,
                error,
                errorMessage )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( errorMessage );
        _errorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorMessage => _errorMessage;

    protected override AbstractNode DoClone( ImmutableArray<Trivia> leading, IList<AbstractNode>? content, ImmutableArray<Trivia> trailing )
    {
        return new TokenErrorNode( TokenType, _errorMessage, leading, trailing );
    }
}
