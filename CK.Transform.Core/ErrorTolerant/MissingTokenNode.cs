using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.ErrorTolerant;

/// <summary>
/// Only <see cref="TokenNode"/> with an empty <see cref="TokenNode.Text"/>.
/// This is not an error (but the source text is invalid) and this node can only belong to a <see cref="SyntaxErrorNode"/>.
/// </summary>
public sealed class MissingTokenNode : TokenNode, IErrorTolerantNode
{
    readonly string _message;

    /// <summary>
    /// Initializes a new missing token node.
    /// </summary>
    /// <param name="message">Required error message.</param>
    /// <param name="tokenType">The token type. Must not be an error.</param>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="trailing">Trailing trivias.</param>
    public MissingTokenNode( string message, TokenType tokenType = TokenType.GenericMissingToken, ImmutableArray<Trivia> leading = default, ImmutableArray<Trivia> trailing = default )
        : base( leading.IsDefault ? [] : leading,
                trailing.IsDefault ? [] : trailing,
                tokenType,
                ReadOnlyMemory<char>.Empty )
    {
        Throw.CheckArgument( !tokenType.IsError() );
        Throw.CheckArgument( !string.IsNullOrWhiteSpace( message ) );
        _message = message;
    }

    /// <summary>
    /// Gets a non empty error message.
    /// </summary>
    public string Message => _message;
}
