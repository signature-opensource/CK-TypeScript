namespace CK.Transform.Core;

/// <summary>
/// Low-level token is a candidate token.
/// </summary>
/// <param name="NodeType">The detected candidate node type. Defaults to <see cref="NodeType.None"/>.</param>
/// <param name="Length">The candidate token length. Defaults to 0.</param>
public readonly record struct LowLevelToken( NodeType NodeType, int Length );
