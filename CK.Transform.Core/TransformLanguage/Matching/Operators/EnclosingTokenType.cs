namespace CK.Transform.Core;

/// <summary>
/// Categorizes tokens for <see cref="EnclosedSpanDeepestEnumerator"/> and <see cref="CoveringEnclosedSpanEnumerator"/>
/// </summary>
public enum EnclosingTokenType
{
    /// <summary>
    /// The token is a ragular one.
    /// </summary>
    None,

    /// <summary>
    /// The token is a starting token.
    /// </summary>
    Open,

    /// <summary>
    /// The token is a closing token.
    /// </summary>
    Close
}
