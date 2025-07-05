namespace CK.Transform.Core;

/// <summary>
/// Decorated token with its index. The only invalid SourceToken is the <c>default</c>,
/// this makes <see cref="System.Nullable{T}"/> useless for this type.
/// </summary>
/// <param name="Token">The token.</param>
/// <param name="Index">The token index.</param>
public readonly record struct SourceToken( Token Token, int Index )
{
    /// <summary>
    /// Gets whether this SourceToken is valid.
    /// The only invalid SourceToken is the <c>default</c>.
    /// </summary>
    public bool IsValid => Token != null;
}


