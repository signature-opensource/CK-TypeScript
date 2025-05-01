using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Captures a transformer function. Unlike other <see cref="SourceSpan"/>, the original <see cref="Text"/>
/// is captured and available.
/// <para>
/// This can only be instantiated by <see cref="TransformerHost.TryParseFunction(IActivityMonitor, string)"/>
/// (or the other parse functions).
/// </para>
/// <para>
/// The <see cref="Language"/> is tied to a <see cref="TransformerHost"/> and cannot be changed,
/// just like the <see cref="Body"/>. The <see cref="Name"/> and <see cref="Target"/> are mutable:
/// they can be resolved through the context.
/// </para>
/// </summary>
public sealed class TransformerFunction : TopLevelSourceSpan
{
    internal TransformerFunction( ReadOnlyMemory<char> text,
                                  int createTokenIndex,
                                  int endTokenIndex,
                                  TransformerHost.Language language,
                                  TransformStatementBlock body,
                                  string? name = null,
                                  string? target = null )
        : base( createTokenIndex, endTokenIndex )
    {
        Text = text;
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
    /// Gets or sets the target address.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets the original text.
    /// </summary>
    public ReadOnlyMemory<char> Text { get; }

    /// <summary>
    /// Gets the transform language.
    /// </summary>
    public TransformerHost.Language Language { get; }

    /// <summary>
    /// Gets the body.
    /// </summary>
    public TransformStatementBlock Body { get; }

    internal void Apply( SourceCodeEditor editor )
    {
        Body.Apply( editor.Monitor, editor );
        if( !editor.HasError && editor.NeedReparse ) editor.Reparse();
    }
}
