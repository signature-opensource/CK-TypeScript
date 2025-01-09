namespace CK.Transform.Core;

/// <summary>
/// Decorated token with its index and deepest span.
/// </summary>
/// <param name="Token">The token.</param>
/// <param name="Span">
/// The deepest span that covers the token.
/// <para>
/// <see cref="SourceSpan.Parent"/> can be used to retrieve all the parent covering spans.
/// </para>
/// </param>
/// <param name="Index">The token index.</param>
public readonly record struct SourceToken( Token Token, SourceSpan? Span, int Index )
{
    /// <summary>
    /// Gets whether this SourceToken is the invalid <c>default</c>.
    /// </summary>
    public bool IsDefault => Token == null;
}


