using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Context for filtering tokens.
/// </summary>
public interface IFilteredTokenOperatorContext
{
    /// <summary>
    /// Gets whether the <see cref="SourceCodeEditor"/> is on error.
    /// </summary>
    bool HasEditorError { get; }

    /// <summary>
    /// Gets the current <see cref="SourceCode.Tokens"/>.
    /// </summary>
    IReadOnlyList<Token> UnfilteredTokens { get; }

    /// <summary>
    /// Gets the previous filtering context.
    /// </summary>
    IFilteredTokenOperatorSourceContext Previous { get; }

    /// <summary>
    /// Signals an operator error.
    /// </summary>
    /// <param name="failureMessage">The error message.</param>
    /// <param name="current">Optional enumerator to dump the error detailed position.</param>
    void SetFailedResult( string failureMessage, IFilteredTokenSpanEnumerator? current );

    /// <summary>
    /// Sets the result of the operator.
    /// </summary>
    /// <param name="result">The result.</param>
    void SetResult( FilteredTokenSpan[] result );

    /// <summary>
    /// Sets the result of the operator.
    /// </summary>
    /// <param name="builder">The result builder.</param>
    void SetResult( FilteredTokenSpanListBuilder builder );

    /// <summary>
    /// Sets a no-op result for the operator.
    /// </summary>
    void SetUnchangedResult();

    /// <summary>
    /// Gets the deepest <see cref="SourceSpan"/> at a token position.
    /// </summary>
    /// <param name="index">The token index.</param>
    /// <returns>The deepest span or null.</returns>
    SourceSpan? GetDeepestSpanAt( int index );

    /// <summary>
    /// Gets the deepest span assignable to a <paramref name="spanType"/>.
    /// </summary>
    /// <param name="index">The token index.</param>
    /// <param name="spanType">Type of the span.</param>
    /// <returns>The deepest span or null.</returns>
    SourceSpan? GetDeepestSpanAt( int index, Type spanType );

    /// <summary>
    /// Gets a shared builder that <see cref="IFilteredTokenOperator.Apply"/> can use.
    /// </summary>
    FilteredTokenSpanListBuilder SharedBuilder { get; }
}
