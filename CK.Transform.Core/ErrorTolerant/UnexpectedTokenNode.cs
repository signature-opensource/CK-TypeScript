using CK.Transform.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.ErrorTolerant;

/// <summary>
/// An unexpected (skipped) token.
/// This is not an error (but the source text is invalid) and this node can only belong to a <see cref="SyntaxErrorNode"/>.
/// </summary>
public sealed class UnexpectedTokenNode : TokenNode, IErrorTolerantNode
{
    /// <summary>
    /// Initializes a new missing token node.
    /// </summary>
    /// <param name="text">Skipped text.</param>
    /// <param name="tokenType">The token type. Must not be an error.</param>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="trailing">Trailing trivias.</param>
    public UnexpectedTokenNode( ReadOnlyMemory<char> text,
                                NodeType tokenType = NodeType.GenericUnexpectedToken,
                                ImmutableArray<Trivia> leading = default,
                                ImmutableArray<Trivia> trailing = default )
        : base( tokenType, text, leading, trailing )
    {
    }
}
