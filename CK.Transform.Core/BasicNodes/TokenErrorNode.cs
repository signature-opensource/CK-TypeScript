using CK.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Can capture any error as a <see cref="TokenNode"/> (hence as an <see cref="AbstractNode"/>): this may
/// appear anywhere in the AST.
/// </summary>
public class TokenErrorNode : TokenNode, IErrorNode
{
    readonly string _errorMessage;
    readonly SourcePosition _sourcePosition;

    internal TokenErrorNode( NodeType error,
                             string errorMessage,
                             SourcePosition sourcePosition,
                             ImmutableArray<Trivia> leading,
                             ImmutableArray<Trivia> trailing )
        : base( leading,
                trailing,
                error,
                errorMessage.AsMemory() )
    {
        Throw.DebugAssert( error.IsError() );
        Throw.DebugAssert( !leading.IsDefault );
        Throw.DebugAssert( !trailing.IsDefault );
        Throw.DebugAssert( !string.IsNullOrWhiteSpace( errorMessage ) );
        _errorMessage = errorMessage;
        _sourcePosition = sourcePosition;
    }

    internal TokenErrorNode( NodeType error,
                             ReadOnlyMemory<char> text,
                             string errorMessage,
                             SourcePosition sourcePosition,
                             ImmutableArray<Trivia> leading,
                             ImmutableArray<Trivia> trailing )
        : base( leading,
                trailing,
                error,
                text )
    {
        Throw.DebugAssert( error.IsError() );
        Throw.DebugAssert( !leading.IsDefault );
        Throw.DebugAssert( !trailing.IsDefault );
        Throw.DebugAssert( !string.IsNullOrWhiteSpace( errorMessage ) );
        _errorMessage = errorMessage;
        _sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorMessage => _errorMessage;

    /// <summary>
    /// Gets the position of the error in the source.
    /// </summary>
    public SourcePosition SourcePosition => _sourcePosition;

    public override string ToString() => $"{_errorMessage} @{_sourcePosition.Line},{_sourcePosition.Column}";

}
