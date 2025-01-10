using CK.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Specialized error <see cref="Token"/>. This can only be created by
/// <see cref="TokenizerHead.AppendError(string, int, TokenType)"/> or
/// <see cref="TokenizerHead.CreateHardError(string, TokenType)"/>.
/// </summary>
public sealed class TokenError : Token
{
    readonly string _errorMessage;
    readonly SourcePosition _sourcePosition;

    internal TokenError( TokenType error,
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

    /// <summary>
    /// Overridden to return the error message and position.
    /// </summary>
    /// <returns>The error message and position.</returns>
    public override string ToString() => $"{_errorMessage} @{_sourcePosition.Line + 1},{_sourcePosition.Column + 1}";

}
