using System;

namespace CK.Transform.Core;

public abstract partial class SourceSpan
{
    internal readonly SourceRoot _root;
    internal SourceSpan? _parent;
    internal ChildrenSet _children;
    internal SourceSpan? _prevSibling;
    internal SourceSpan? _nextSibling;
    internal TokenSpan _span;

    public SourceSpan( SourceRoot root, int index, int width )
    {
        _root = root;
        _span = new TokenSpan( index, index + width );
        _children = new ChildrenSet();
    }

    /// <summary>
    /// Gets the root.
    /// </summary>
    public SourceRoot Root => _root;

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
    public ChildrenSet Children => _children;

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
