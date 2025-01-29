namespace CK.Less.Transform;

/// <summary>
/// Defines the possible <c>@import (keyword) 'file';</c>.
/// </summary>
[Flags]
public enum ImportKeyword
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// Use a Less file but do not output it.
    /// </summary>
    Reference,

    /// <summary>
    /// Include the source file in the output but do not process it.
    /// </summary>
    Inline,

    /// <summary>
    /// Treat the file as a Less file, no matter what the file extension.
    /// This excludes <see cref="Css"/>.
    /// </summary>
    Less,

    /// <summary>
    /// Treat the file as a CSS file, no matter what the file extension.
    /// This excludes <see cref="Less"/>.
    /// </summary>
    Css,

    /// <summary>
    /// Only include the file once (this is default behavior).
    /// This excludes <see cref="Multiple"/>.
    /// </summary>
    Once,

    /// <summary>
    /// Include the file multiple times.
    /// This excludes <see cref="Once"/>.
    /// </summary>
    Multiple,

    /// <summary>
    /// Continue compiling when file is not found.
    /// </summary>
    Optional
}
