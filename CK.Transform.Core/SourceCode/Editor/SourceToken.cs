namespace CK.Transform.Core;

/// <summary>
/// Decorated token with its index.
/// </summary>
/// <param name="Token">The token.</param>
/// <param name="Index">The token index.</param>
public readonly record struct SourceToken( Token Token, int Index )
{
    /// <summary>
    /// Gets whether this SourceToken is the invalid <c>default</c>.
    /// </summary>
    public bool IsDefault => Token == null;
}


