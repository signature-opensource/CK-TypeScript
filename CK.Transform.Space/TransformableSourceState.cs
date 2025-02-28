namespace CK.Transform.Space;

/// <summary>
/// State of a <see cref="TransformableSource"/>.
/// </summary>
public enum TransformableSourceState
{
    /// <summary>
    /// The <see cref="TransformableSource.Origin"/> has changed or
    /// has never been parsed.
    /// </summary>
    Dirty,

    /// <summary>
    /// The resource content cannot be parsed.
    /// </summary>
    SyntaxError,

    /// <summary>
    /// The resource content has been successfully parsed.
    /// </summary>
    Parsed
}
