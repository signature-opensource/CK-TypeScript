using CK.Core;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Can capture any error as a <see cref="TokenNode"/> (hence as an <see cref="AbstractNode"/>): this may
/// appear anywhere in the AST.
/// </summary>
public class TokenErrorNode : TokenNode
{
    readonly string _errorMessage;

    /// <summary>
    /// Singleton for "Unhandled" scenario (a gentle error).
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
}
