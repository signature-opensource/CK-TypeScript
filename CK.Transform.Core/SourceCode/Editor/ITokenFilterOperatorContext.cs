using System;
using System.Collections.Generic;

namespace CK.Transform.Core;

/// <summary>
/// Context for filtering tokens.
/// </summary>
public interface ITokenFilterOperatorContext
{
    /// <summary>
    /// Gets the current <see cref="SourceCode.Tokens"/>.
    /// </summary>
    IReadOnlyList<Token> UnfilteredTokens { get; }

    /// <summary>
    /// Signals an operator error.
    /// </summary>
    /// <param name="failureMessage">The error message.</param>
    /// <param name="current">Optional enumerator to dump the error detailed position.</param>
    void SetFailedResult( string failureMessage, ITokenFilterEnumerator? current );

    /// <summary>
    /// Sets the result of the operator.
    /// </summary>
    /// <param name="result">The result.</param>
    void SetResult( TokenMatch[] result );

    /// <summary>
    /// Sets the result of the operator.
    /// </summary>
    /// <param name="builder">The result builder.</param>
    void SetResult( TokenFilterBuilder builder );

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
    /// <para>
    /// Unlike <see cref="GetTopSpanAt(int, Type, TokenSpan)"/> this method has no scope
    /// restriction because it can be done at the call site because there are no other candidates
    /// than the returned span: scoping it is simply rejecting it.
    /// </para>
    /// </summary>
    /// <param name="index">The token index.</param>
    /// <param name="spanType">Type of the span.</param>
    /// <returns>The deepest span or null.</returns>
    SourceSpan? GetDeepestSpanAt( int index, Type spanType );

    /// <summary>
    /// Gets the widest span assignable to a <paramref name="spanType"/> in a given <paramref name="scope"/>.
    /// </summary>
    /// <param name="index">The token index.</param>
    /// <param name="spanType">Type of the span.</param>
    /// <param name="scope">Restricts the result to be in this span. Use <see cref="TokenSpan.Empty"/> for no scope.</param>
    /// <returns>The deepest span or null.</returns>
    SourceSpan? GetTopSpanAt( int index, Type spanType, TokenSpan scope );

    /// <summary>
    /// Gets a shared builder that <see cref="ITokenFilterOperator.Apply"/> can use.
    /// </summary>
    TokenFilterBuilder SharedBuilder { get; }
}
