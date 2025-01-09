using CK.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

public abstract partial class SourceSpan
{
    internal SourceSpanRoot? _root;
    internal SourceSpan? _parent;
    internal SourceSpanChildren _children;
    internal SourceSpan? _prevSibling;
    internal SourceSpan? _nextSibling;
    internal TokenSpan _span;

    /// <summary>
    /// Initializes a new SourceSpan.
    /// </summary>
    /// <param name="beg">The start of the span. Must be greater or equal to 0.</param>
    /// <param name="end">The end of the span. Must be greater than <paramref name="beg"/>.</param>
    public SourceSpan( int beg, int end )
    {
        _span = new TokenSpan( beg, end );
        _children = new SourceSpanChildren();
    }

    /// <summary>
    /// Gets the root. Null when <see cref="IsDetached"/> is true.
    /// </summary>
    public SourceSpanRoot? Root => _root;

    /// <summary>
    /// Gets whether this span doesn't belong to a <see cref="SourceCode"/>.
    /// </summary>
    [MemberNotNullWhen( false, nameof( Root ), nameof( _root ) )]
    public bool IsDetached => _root == null;

    /// <summary>
    /// Detaches this span from the <see cref="Root"/>.
    /// </summary>
    /// <param name="withChildren">
    /// True to also detach the <see cref="Children"/> instead of lifting them
    /// as children of this <see cref="Parent"/>.
    /// </param>
    public void Detach( bool withChildren )
    {
        if( _root != null )
        {
            var pC = _parent != null ? _parent._children : _root._children;
            if( withChildren )
            {
                _children.Clear();
            }
            else if( _children.HasChildren ) 
            {
                SourceSpanChildren.SetParentFrom( _children.FirstChild, _parent );
                _nextSibling = _children.FirstChild;
                _prevSibling = _children.LastChild;
            }
            if( _prevSibling == null )
            {
                pC._firstChild = _nextSibling;
            }
            else
            {
                _prevSibling._nextSibling = _nextSibling;
            }
            _nextSibling = null;
            if( _nextSibling == null )
            {
                pC._lastChild = _prevSibling;
            }
            else
            {
                _nextSibling._prevSibling = _prevSibling;
            }
            _prevSibling = null;
            _parent = null;
            _root = null;
        }
    }
    /// <summary>
    /// Gets the token span.
    /// </summary>
    public TokenSpan Span => _span;

    /// <summary>
    /// Gets the parent span. Null when this span is a top-level one, directly under the <see cref="Root"/>.
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

    internal SourceSpan GetSpanAt( int index )
    {
        Throw.DebugAssert( Span.Contains( index ) );
        return _children.GetSpanAt( index ) ?? this;
    }

}
