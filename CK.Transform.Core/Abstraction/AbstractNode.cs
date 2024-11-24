using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Base class of all possible <see cref="TokenNode"/> and <see cref="CompositeNode"/>.
/// </summary>
public abstract partial class AbstractNode : IAbstractNode
{
    readonly ImmutableArray<Trivia> _leadingTrivias;
    readonly ImmutableArray<Trivia> _trailingTrivias;

    private protected AbstractNode( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing )
    {
        _leadingTrivias = leading;
        _trailingTrivias = trailing;
    }

    void IAbstractNode.ExternalImplementationsDisabled() { }

    /// <inheritdoc />
    public abstract IReadOnlyList<AbstractNode> ChildrenNodes { get; }

    /// <inheritdoc />
    public ImmutableArray<Trivia> LeadingTrivias => _leadingTrivias;

    /// <inheritdoc />
    public abstract IEnumerable<Trivia> FullLeadingTrivias { get; }

    /// <inheritdoc />
    public ImmutableArray<Trivia> TrailingTrivias => _trailingTrivias;

    /// <inheritdoc />
    public abstract IEnumerable<Trivia> FullTrailingTrivias { get; }

    /// <inheritdoc />
    public abstract IEnumerable<AbstractNode> LeadingNodes { get; }

    /// <inheritdoc />
    public abstract IEnumerable<AbstractNode> TrailingNodes { get; }

    /// <inheritdoc />
    public abstract IEnumerable<TokenNode> AllTokens { get; }

    /// <inheritdoc />
    public abstract int Width { get; }

    /// <inheritdoc />
    public abstract TokenType TokenType { get; }

    /// <summary>
    /// Finds the token at a relative position in this node. if the index is out of 
    /// range (ie. negative or greater or equal to <see cref="Width"/>), null is returned.
    /// </summary>
    /// <param name="index">The zero based index of the token to locate.</param>
    /// <param name="onPath">Will be called for each intermediate node with the relative index of its first token.</param>
    /// <returns>The token or null if index is out of range.</returns>
    public TokenNode? LocateToken( int index, Action<IAbstractNode, int> onPath )
    {
        if( index < 0 || index >= Width ) return null;

        if( this is TokenNode result ) return result;

        AbstractNode n = this;
        int cPos = 0;
        for(; ; )
        {
            var children = n.ChildrenNodes;
            Throw.DebugAssert( children.Count != 0 );
            foreach( var c in children )
            {
                int cW = c.Width;
                if( index < cW )
                {
                    if( c is TokenNode r ) return r;
                    onPath( c, cPos );
                    n = Unsafe.As<AbstractNode>( c );
                    break;
                }
                cPos += cW;
                index -= cW;
                Debug.Assert( index >= 0 );
            }
        }
    }

    public int LocateDirectChildIndex( ref int index )
    {
        int idx = -1;
        if( index >= 0 && index < Width )
        {
            int cPos = 0;
            var children = ChildrenNodes;
            foreach( var c in children )
            {
                ++idx;
                int cW = c.Width;
                if( index < cW ) break;
                cPos += cW;
                index -= cW;
                Debug.Assert( index >= 0 );
            }
        }
        return idx;
    }


    /// <summary>
    /// Extracts a mutable copy of the children. This list is either:
    /// <list type="bullet">
    ///     <item>A truly dynamic <see cref="List{T}"/> with non null AbstractNodes in it.</item>
    ///     <item>An array with potentially null AbstractNodes that may be changed but its length cannot change.</item>
    /// </list>
    /// </summary>
    /// <returns>A <c>List&lt;AbstractNode&gt;</c> or a <c>AbstractNode?[]</c> array.</returns>
    public abstract IList<AbstractNode> GetRawContent();

    /// <summary>
    /// Fundamental method that rebuilds this Node with new trivias and content.
    /// <para>
    /// This is called only if a mutation is required (the trivias or the content have changed).
    /// </para>
    /// </summary>
    /// <param name="leading">Leading trivias.</param>
    /// <param name="content">New content or null if the content did not change. See <see cref="GetRawContent()"/>.</param>
    /// <param name="trailing">Trailing trivias.</param>
    /// <returns>A new immutable object.</returns>
    protected abstract AbstractNode DoClone( ImmutableArray<Trivia> leading, IList<AbstractNode>? content, ImmutableArray<Trivia> trailing );

    internal AbstractNode DoLiftLeadingTrivias() => DoLift( ImmutableArray.CreateBuilder<Trivia>(), null, this, true );

    internal AbstractNode DoLiftTrailingTrivias() => DoLift( null, ImmutableArray.CreateBuilder<Trivia>(), this, true );

    internal AbstractNode DoLiftBothTrivias() => DoLift( ImmutableArray.CreateBuilder<Trivia>(), ImmutableArray.CreateBuilder<Trivia>(), this, true );

    static AbstractNode DoLift( ImmutableArray<Trivia>.Builder? hL, ImmutableArray<Trivia>.Builder? tL, AbstractNode n, bool root )
    {
        hL?.AddRange( n.LeadingTrivias );
        IList<AbstractNode>? content = n.GetRawContent();
        bool contentChanged = false;
        int nbC = content.Count;
        if( nbC > 0 )
        {
            int idx;
            if( nbC == 1 || hL != null )
            {
                AbstractNode? firstChild = RawGetFirstChildInContent( content, out idx );
                if( firstChild != null )
                {
                    contentChanged = RawReplaceContentNode( content, idx, DoLift( hL, nbC == 1 ? tL : null, firstChild, false ) ) != null;
                }
            }
            if( nbC > 1 && tL != null )
            {
                AbstractNode? lastChild = RawGetLastChildInContent( content, out idx );
                if( lastChild != null )
                {
                    contentChanged |= RawReplaceContentNode( content, idx, DoLift( null, tL, lastChild, false ) ) != null;
                }
            }
        }
        if( !contentChanged ) content = null;
        tL?.AddRange( n._trailingTrivias );
        AbstractNode sN = (AbstractNode)n;
        return root
                ? sN.InternalDoClone(
                        hL != null ? hL.ToImmutableArray() : n.LeadingTrivias,
                        content,
                        tL != null ? tL.ToImmutableArray() : n.TrailingTrivias )
                : sN.InternalDoClone(
                        hL != null ? ImmutableArray<Trivia>.Empty : n.LeadingTrivias,
                        content,
                        tL != null ? ImmutableArray<Trivia>.Empty : n.TrailingTrivias );
    }

    internal AbstractNode DoSetTrivias( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing )
    {
        if( leading == null ) leading = ImmutableArray<Trivia>.Empty;
        if( trailing == null ) trailing = ImmutableArray<Trivia>.Empty;
        if( leading != _leadingTrivias
            && leading.AsSpan().SequenceEqual( _leadingTrivias.AsSpan() ) )
        {
            leading = LeadingTrivias;
        }
        if( trailing != TrailingTrivias
            && trailing.AsSpan().SequenceEqual( TrailingTrivias.AsSpan() ) )
        {
            trailing = TrailingTrivias;
        }
        return leading != _leadingTrivias || trailing != _trailingTrivias
                ? DoClone( leading, null, trailing )
                : this;
    }

    internal AbstractNode DoExtractTrailingTrivias( Func<Trivia, int, bool> predicate )
    {
        int nb = TrailingTrivias.Length;
        int keep;
        if( (keep = nb) != 0 )
        {
            for( int i = _trailingTrivias.Length - 1; i >= 0; i-- )
            {
                if( !predicate( _trailingTrivias[i], --keep ) ) break;
            }
        }
        if( keep == 0 )
        {
            IList<AbstractNode>? content = GetRawContent();
            AbstractNode? c = RawGetLastChildInContent( content, out int idx );
            if( c != null )
            {
                content = RawReplaceContentNode( content, idx, c.DoExtractTrailingTrivias( predicate ) );
            }
            else
            {
                if( nb == 0 ) return this;
                content = null;
            }
            return DoClone( LeadingTrivias, content, ImmutableArray<Trivia>.Empty );
        }
        else if( keep != nb )
        {
            return DoClone( LeadingTrivias, null, TrailingTrivias.RemoveRange( nb - keep, keep ) );
        }
        return this;
    }

    internal AbstractNode DoExtractLeadingTrivias( Func<Trivia, int, bool> filter )
    {
        int nb = _leadingTrivias.Length;
        int keep;
        if( (keep = nb) != 0 )
        {
            int idx = 0;
            foreach( var t in _leadingTrivias )
            {
                if( !filter( t, idx++ ) ) break;
                --keep;
            }
        }
        if( keep == 0 )
        {
            IList<AbstractNode>? content = GetRawContent();
            AbstractNode? c = RawGetFirstChildInContent( content, out int idx );
            if( c != null )
            {
                content = RawReplaceContentNode( content, idx, c.DoExtractLeadingTrivias( filter ) );
            }
            else
            {
                if( nb == 0 ) return this;
                content = null;
            }
            return DoClone( ImmutableArray<Trivia>.Empty, content, TrailingTrivias );
        }
        else if( keep != nb )
        {
            return DoClone( LeadingTrivias.RemoveRange( 0, nb - keep ), null, TrailingTrivias );
        }
        return this;
    }

    internal AbstractNode DoSetRawContent( IList<AbstractNode> childrenNodes )
    {
        if( childrenNodes == null ) childrenNodes = Array.Empty<AbstractNode>();
        return DoClone( LeadingTrivias, childrenNodes, TrailingTrivias );
    }

    internal AbstractNode DoReplaceContentNode( int i, AbstractNode child )
    {
        var c = RawReplaceContentNode( GetRawContent(), i, child );
        return c != null ? DoClone( LeadingTrivias, c, TrailingTrivias ) : this;
    }

    internal AbstractNode DoReplaceContentNode( Func<AbstractNode, int, int, AbstractNode> replacer )
    {
        bool change = false;
        var list = GetRawContent();
        var pos = 0;
        for( int i = 0; i < list.Count; ++i )
        {
            var current = list[i];
            var replaced = replacer( current, pos, i );
            if( replaced != null || list is AbstractNode[] )
            {
                if( current != replaced )
                {
                    change = true;
                    list[i] = replaced!;
                }
            }
            else
            {
                change = true;
                list.RemoveAt( i-- );
            }
            if( current != null ) pos += current.Width;
        }
        return change ? DoClone( LeadingTrivias, list, TrailingTrivias ) : this;
    }

    internal AbstractNode DoReplaceContentNode( int i1, AbstractNode child1, int i2, AbstractNode child2 )
    {
        var c = RawReplaceContentNode( GetRawContent(), i1, child1, i2, child2 );
        return c != null ? DoClone( LeadingTrivias, c, TrailingTrivias ) : this;
    }

    internal AbstractNode DoStuffRawContent( int iStart, int count, IReadOnlyList<AbstractNode> children )
    {
        Throw.CheckNotNullArgument( children );
        IList<AbstractNode> c = GetRawContent();
        RawStuffContent( c, iStart, count, children );
        return DoClone( LeadingTrivias, c, TrailingTrivias );
    }

    static IList<AbstractNode>? RawReplaceContentNode( IList<AbstractNode> content, int i, AbstractNode? child )
    {
        if( child != null || content is AbstractNode[] )
        {
            if( content[i] == child ) return null;
            content[i] = child!;
        }
        else content.RemoveAt( i );
        return content;
    }

    static IList<AbstractNode>? RawReplaceContentNode( IList<AbstractNode> content, int i1, AbstractNode? child1, int i2, AbstractNode? child2 )
    {
        if( (child1 != null && child2 != null) || content is AbstractNode[] )
        {
            if( content[i1] == child1 && content[i2] == child2 ) return null;
            content[i1] = child1!;
            content[i2] = child2!;
        }
        else
        {
            if( child1 == null )
            {
                content.RemoveAt( i1 );
                if( i1 < i2 ) --i2;
            }
            else content[i1] = child1;

            if( child2 == null ) content.RemoveAt( i2 );
            else content[i2] = child2;
        }
        return content;
    }

    static AbstractNode? RawGetFirstChildInContent( IList<AbstractNode> content, out int idx )
    {
        AbstractNode? firstChild = null;
        for( idx = 0; idx < content.Count; ++idx )
            if( (firstChild = content[idx]) != null ) break;
        return firstChild;
    }

    static AbstractNode? RawGetLastChildInContent( IList<AbstractNode> content, out int idx )
    {
        AbstractNode? lastChild = null;
        for( idx = content.Count - 1; idx >= 0; --idx )
            if( (lastChild = content[idx]) != null ) break;
        return lastChild;
    }

    static IList<AbstractNode>? RawStuffContent( IList<AbstractNode> content, int iStart, int count, IReadOnlyList<AbstractNode> children )
    {
        List<AbstractNode>? lC = content as List<AbstractNode>;
        if( lC == null || children.Count == count )
        {
            Throw.DebugAssert( lC == null || content is AbstractNode[] );
            bool changed = false;
            for( int i = 0; i < count; ++i )
            {
                if( content[iStart + i] != children[i] )
                {
                    content[iStart + i] = children[i];
                    changed = true;
                }
            }
            return changed ? content : null;
        }
        Throw.DebugAssert( lC != null );
        lC.RemoveRange( iStart, count );
        lC.InsertRange( iStart, children );
        return content;
    }

    internal AbstractNode DoAddLeadingTrivia( Trivia t, Func<Trivia, bool>? skipper )
    {
        if( !t.IsValid ) return this;
        int i = 0;
        if( skipper != null )
        {
            foreach( var p in LeadingTrivias )
            {
                if( !skipper( p ) ) break;
                ++i;
            }
        }
        return DoClone( LeadingTrivias.Insert( i, t ), null, TrailingTrivias );
    }

    internal AbstractNode DoAddTrailingTrivia( Trivia t, Func<Trivia, bool>? skipper )
    {
        if( !t.IsValid ) return this;
        int count = _trailingTrivias.Length;
        int idx = count;
        if( skipper != null )
        {
            for( int i = 0; i < count; ++i )
            {
                if( !skipper( _trailingTrivias[idx - 1] ) ) break;
                --idx;
            }
        }
        return DoClone( _leadingTrivias, null, _trailingTrivias.Insert( idx, t ) );
    }

    internal AbstractNode InternalDoClone( ImmutableArray<Trivia> leading, IList<AbstractNode>? content, ImmutableArray<Trivia> trailing )
    {
        return leading == _leadingTrivias && content == null && trailing == _trailingTrivias
                ? this
                : DoClone( leading, content, trailing );
    }



}
