using CK.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Span of a source code. Specialized instances are created by analyzers and
/// managed by <see cref="SourceCode"/> and <see cref="SourceCodeEditor"/>.
/// </summary>
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
    protected SourceSpan( int beg, int end )
    {
        _span = new TokenSpan( beg, end );
        _children = new SourceSpanChildren();
    }

    /// <summary>
    /// Gets the root. Null when <see cref="IsDetached"/> is true.
    /// </summary>
    public ISourceSpanRoot? Root => _root;

    /// <summary>
    /// Gets whether this span doesn't belong to a <see cref="SourceCode"/>.
    /// </summary>
    [MemberNotNullWhen( false, nameof( Root ), nameof( _root ) )]
    public bool IsDetached => _root == null;

    /// <summary>
    /// Checks whether this span is valid. At this level, it must not be <see cref="IsDetached"/>
    /// and all children's <see cref="SourceSpanChildren.CheckValid()"/> must return true.
    /// <para>
    /// This is often specialized to implement checks on <see cref="Children"/>, <see cref="Parent"/>
    /// or siblings. Specializations must call this base method.
    /// </para>
    /// <para>
    /// Overrides typically specify [MemberNotNullWhen( true, ...)] with required properties of the structure.
    /// This has been designed to be used in <see cref="Throw.DebugAssert(bool, string?, string?, int)"/>:
    /// spans must always be valid, see <see cref="Bind"/>.
    /// </para>
    /// </summary>
    /// <returns>True if this span is valid.</returns>
    [MemberNotNullWhen( true, nameof( Root ), nameof( _root ) )]
    public virtual bool CheckValid()
    {
        return !IsDetached && Children.CheckValid();
    }

    /// <summary>
    /// Binds this span to the <see cref="BindingContext.SpanTokens"/> and to its
    /// <see cref="Children"/>. Errors are signaled with <see cref="BindingContext.AddError(Token, string, bool)"/>.
    /// <para>
    /// Once any Bind fails, <see cref="CheckValid"/> should return false on this span (and its parents) but nothing
    /// enforce this. The rule is that a binding failure that occurs during parsing prevents further bindings (but doesn't
    /// stop the parsing) and a binding failure during transformation prevents subsequent transformations to be applied.
    /// </para>
    /// </summary>
    /// <param name="c">The binding context.</param>
    internal protected abstract void Bind( BindingContext c );

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
            if( _nextSibling == null )
            {
                pC._lastChild = _prevSibling;
            }
            else
            {
                _nextSibling._prevSibling = _prevSibling;
            }
            _nextSibling = null;
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
    /// Gets the previous sibling.
    /// </summary>
    public SourceSpan? PreviousSibling => _prevSibling;

    /// <summary>
    /// Gets the next sibling.
    /// </summary>
    public SourceSpan? NextSibling => _nextSibling;

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
