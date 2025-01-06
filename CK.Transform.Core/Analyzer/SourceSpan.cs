using System;

namespace CK.Transform.Core;

public abstract partial class SourceSpan
{
    internal readonly SourceCode _root;
    internal SourceSpan? _parent;
    internal SourceSpanChildren _children;
    internal SourceSpan? _prevSibling;
    internal SourceSpan? _nextSibling;
    internal TokenSpan _span;

    public SourceSpan( SourceCode root, int index, int width )
    {
        _root = root;
        _span = new TokenSpan( index, index + width );
        _children = new SourceSpanChildren();
        root.Add( this );
    }

    /// <summary>
    /// Gets the root.
    /// </summary>
    public SourceCode Root => _root;

    /// <summary>
    /// Gets the token span.
    /// </summary>
    public TokenSpan Span => _span;

    /// <summary>
    /// Gets the parent span. Null for the root.
    /// </summary>
    public SourceSpan? Parent => _parent;

    /// <summary>
    /// Gets the children.
    /// </summary>
    public SourceSpanChildren Children => _children;

    /// <summary>
    /// Gets the type name of this span. This should be compared using <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// <para>
    /// Defaults to this type name.
    /// </para>
    /// </summary>
    public virtual string TypeName => GetType().Name;

    /// <summary>
    /// Overridden to return the <see cref="TypeName"/> and the <see cref="Span"/>.
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString() => $"{TypeName} {_span}";
}
