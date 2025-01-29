using CK.Core;

namespace CK.Transform.Core;

public sealed class TransformerFunction : SourceSpan
{
    public TransformerFunction( int createTokenIndex,
                                int endTokenIndex,
                                TransformLanguage language,
                                TransformStatementBlock statements,
                                string? name = null,
                                string? target = null )
        : base( createTokenIndex, endTokenIndex )
    {
        Language = language;
        Name = name;
        Target = target;
        Body = statements;
    }

    /// <summary>
    /// Gets or sets this transformer function name.
    /// </summary>
    public string? Name { get; set; }

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
