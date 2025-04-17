using CK.Core;

namespace CK.Transform.Core;

/// <summary>
/// Captures a transformer function.
/// </summary>
public sealed class TransformerFunction : TopLevelSourceSpan
{
    /// <summary>
    /// Initializes a new transfomer function.
    /// </summary>
    /// <param name="createTokenIndex">The start of the span. Must be greater or equal to 0.</param>
    /// <param name="endTokenIndex">The end of the span. Must be greater than <paramref name="createTokenIndex"/>.</param>
    /// <param name="language">The target transform language.</param>
    /// <param name="body">The transform statements.</param>
    /// <param name="name">Optional treansform function name.</param>
    /// <param name="target">Optional treansform function target.</param>
    public TransformerFunction( int createTokenIndex,
                                int endTokenIndex,
                                TransformLanguage language,
                                TransformStatementBlock body,
                                string? name = null,
                                string? target = null )
        : base( createTokenIndex, endTokenIndex )
    {
        Language = language;
        Name = name;
        Target = target;
        Body = body;
    }

    /// <summary>
    /// Gets or sets this transformer function name.
    /// </summary>
    public override string? Name { get; set; }

    /// <summary>
    /// Gets or sets the transform language.
    /// </summary>
    public TransformLanguage Language { get; set; }

    /// <summary>
    /// Gets or sets the target address.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets the body.
    /// </summary>
    public TransformStatementBlock Body { get; }

    /// <summary>
    /// Applies the <see cref="Body"/> to the <paramref name="editor"/>.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="editor">The code to transform.</param>
    /// <returns>True on success, false on error.</returns>
    public bool Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        return Body.Apply( monitor, editor ) && (!editor.NeedReparse || editor.Reparse( monitor ));
    }
}
