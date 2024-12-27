using CK.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.Transform.Core;

public sealed class SourceRoot
{
    SourceSpan.ChildrenSet _children;
    int _width;

    public SourceRoot()
    {
        _children = new SourceSpan.ChildrenSet();
    }

    public int Width { get => _width; set => _width = value; }

    public void SetWidth( int width )
    {
        _width = width;
    }

    internal bool TryAdd( SourceSpan newOne )
    {
        Throw.DebugAssert( newOne._root == this );
        if( _children._firstChild == null )
        {
            _children._firstChild = _children._lastChild = newOne;
        }
        var cover = _children.CoveringSpan;
        if( cover.IsEmpty || cover.Contains( newOne.Span ) )
        {
            return _children.TryAddBelow( null, newOne );
        }

        {
            Throw.ArgumentException( nameof( newOne ), $"Invalid new '{newOne}'." );
        }
    }

    /// <summary>
    /// Gets the children.
    /// </summary>
    public SourceSpan.ChildrenSet Children => _children;


}
