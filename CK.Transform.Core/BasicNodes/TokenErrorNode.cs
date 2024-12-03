using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.Core;

/// <summary>
/// Can capture any error as a <see cref="TokenNode"/> (hence as an <see cref="AbstractNode"/>): this may
/// appear anywhere in the AST.
/// <para>
/// This can only be created by <see cref="Analyzer.CreateError(string, NodeType)"/>.
/// </para>
/// </summary>
public class TokenErrorNode : TokenNode
{
    readonly string _errorMessage;
    readonly SourcePosition _sourcePosition;

    /// <summary>
    /// Singleton for "Unhandled" scenario (a gentle error). This error is always at 0,0 and hase the "Unhandled" error message. 
    /// </summary>
    public static readonly TokenErrorNode Unhandled = new TokenErrorNode( NodeType.ErrorUnhandled,
                                                                          "Unhandled",
                                                                          default,
                                                                          ImmutableArray<Trivia>.Empty,
                                                                          ImmutableArray<Trivia>.Empty );

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

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorMessage => _errorMessage;

    /// <summary>
    /// Gets the position of the error in the source.
    /// </summary>
    public SourcePosition SourcePosition => _sourcePosition;
}
