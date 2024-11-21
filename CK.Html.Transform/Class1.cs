//using System.Collections.Immutable;
//using System.Diagnostics;
//using System.Text;

//namespace CK.Html.Transform;

///// <summary>
///// Base class of all nodes.
///// </summary>
//public abstract partial class SqlNode
//{
//    protected SqlNode( ImmutableList<SqlTrivia> leading = null, ImmutableList<SqlTrivia> trailing = null )
//    {
//        LeadingTrivias = leading ?? ImmutableList<SqlTrivia>.Empty;
//        TrailingTrivias = trailing ?? ImmutableList<SqlTrivia>.Empty;
//    }

//    public abstract IReadOnlyList<SqlNode> ChildrenNodes { get; }

//    public abstract IList<SqlNode> GetRawContent();

//    public ImmutableList<SqlTrivia> LeadingTrivias { get; }

//    public ImmutableList<SqlTrivia> TrailingTrivias { get; }

//    public abstract IEnumerable<SqlNode> LeadingNodes { get; }

//    public abstract IEnumerable<SqlNode> TrailingNodes { get; }

//    public abstract IEnumerable<SqlToken> AllTokens { get; }

//    public abstract IEnumerable<SqlTrivia> FullLeadingTrivias { get; }

//    public abstract IEnumerable<SqlTrivia> FullTrailingTrivias { get; }

//    public abstract int Width { get; }

//    public abstract bool IsToken( SqlTokenType t );

//    public SqlToken LocateToken( int index, Action<SqlNode, int> onPath )
//    {
//        if( index < 0 || index >= Width ) return null;

//        SqlToken result = this as SqlToken;
//        if( result != null ) return result;

//        SqlNode n = this;
//        int cPos = 0;
//        for(; ; )
//        {
//            Debug.Assert( n.ChildrenNodes.Count != 0 || (index == 0 && n is SqlToken) );
//            var children = n.ChildrenNodes;
//            Debug.Assert( children.Count != 0 );
//            foreach( var c in children )
//            {
//                int cW = c.Width;
//                if( index < cW )
//                {
//                    result = c as SqlToken;
//                    if( result != null ) return result;
//                    onPath( c, cPos );
//                    n = c;
//                    break;
//                }
//                cPos += cW;
//                index -= cW;
//                Debug.Assert( index >= 0 );
//            }
//        }
//    }

//    public int LocateDirectChildIndex( ref int index )
//    {
//        int idx = -1;
//        if( index >= 0 && index < Width )
//        {
//            int cPos = 0;
//            var children = ChildrenNodes;
//            foreach( var c in children )
//            {
//                ++idx;
//                int cW = c.Width;
//                if( index < cW ) break;
//                cPos += cW;
//                index -= cW;
//                Debug.Assert( index >= 0 );
//            }
//        }
//        return idx;
//    }

//    SqlNode DoLift( ImmutableList<SqlTrivia>.Builder hL, ImmutableList<SqlTrivia>.Builder tL, SqlNode n, bool root )
//    {
//        if( hL != null ) hL.AddRange( n.LeadingTrivias );
//        IList<SqlNode> content = n.GetRawContent();
//        bool contentChanged = false;
//        int nbC = content.Count;
//        if( nbC > 0 )
//        {
//            int idx;
//            if( nbC == 1 || hL != null )
//            {
//                SqlNode firstChild = RawGetFirstChildInContent( content, out idx );
//                if( firstChild != null )
//                {
//                    contentChanged = RawReplaceContentNode( content, idx, DoLift( hL, nbC == 1 ? tL : null, firstChild, false ) ) != null;
//                }
//            }
//            if( nbC > 1 && tL != null )
//            {
//                SqlNode lastChild = RawGetLastChildInContent( content, out idx );
//                if( lastChild != null )
//                {
//                    contentChanged |= RawReplaceContentNode( content, idx, DoLift( null, tL, lastChild, false ) ) != null;
//                }
//            }
//        }
//        if( !contentChanged ) content = null;
//        if( tL != null ) tL.AddRange( n.TrailingTrivias );
//        SqlNode sN = (SqlNode)n;
//        return root
//                ? sN.InternalDoClone(
//                        hL != null ? hL.ToImmutableList() : n.LeadingTrivias,
//                        content,
//                        tL != null ? tL.ToImmutableList() : n.TrailingTrivias )
//                : sN.InternalDoClone(
//                        hL != null ? ImmutableList<SqlTrivia>.Empty : n.LeadingTrivias,
//                        content,
//                        tL != null ? ImmutableList<SqlTrivia>.Empty : n.TrailingTrivias );
//    }

//    internal SqlNode DoLiftLeadingTrivias()
//    {
//        return DoLift( ImmutableList.CreateBuilder<SqlTrivia>(), null, this, true );
//    }

//    internal SqlNode DoLiftTrailingTrivias()
//    {
//        return DoLift( null, ImmutableList.CreateBuilder<SqlTrivia>(), this, true );
//    }

//    internal SqlNode DoLiftBothTrivias()
//    {
//        return DoLift( ImmutableList.CreateBuilder<SqlTrivia>(), ImmutableList.CreateBuilder<SqlTrivia>(), this, true );
//    }

//    internal SqlNode DoSetTrivias( ImmutableList<SqlTrivia> leading, ImmutableList<SqlTrivia> trailing )
//    {
//        if( leading == null ) leading = ImmutableList<SqlTrivia>.Empty;
//        if( trailing == null ) trailing = ImmutableList<SqlTrivia>.Empty;
//        if( leading != LeadingTrivias
//            && leading.Count == LeadingTrivias.Count
//            && leading.SequenceEqual( LeadingTrivias ) )
//        {
//            leading = LeadingTrivias;
//        }
//        if( trailing != TrailingTrivias
//            && trailing.Count == TrailingTrivias.Count
//            && trailing.SequenceEqual( TrailingTrivias ) )
//        {
//            trailing = TrailingTrivias;
//        }
//        return leading != LeadingTrivias || trailing != TrailingTrivias
//                ? DoClone( leading, null, trailing )
//                : this;
//    }

//    internal SqlNode DoExtractTrailingTrivias( Func<SqlTrivia, int, bool> predicate )
//    {
//        int nb = TrailingTrivias.Count;
//        int keep;
//        if( (keep = nb) != 0 )
//        {
//            foreach( var t in TrailingTrivias.Reverse() )
//            {
//                if( !predicate( t, --keep ) ) break;
//            }
//        }
//        if( keep == 0 )
//        {
//            IList<SqlNode> content = GetRawContent();
//            int idx;
//            SqlNode c = RawGetLastChildInContent( content, out idx );
//            if( c != null )
//            {
//                content = RawReplaceContentNode( content, idx, c.ExtractTrailingTrivias( predicate ) );
//            }
//            else
//            {
//                if( nb == 0 ) return this;
//                content = null;
//            }
//            return DoClone( LeadingTrivias, content, ImmutableList<SqlTrivia>.Empty );
//        }
//        else if( keep != nb )
//        {
//            return DoClone( LeadingTrivias, null, TrailingTrivias.RemoveRange( nb - keep, keep ) );
//        }
//        return this;
//    }

//    internal SqlNode DoExtractLeadingTrivias( Func<SqlTrivia, int, bool> filter )
//    {
//        int nb = LeadingTrivias.Count;
//        int keep;
//        if( (keep = nb) != 0 )
//        {
//            int idx = 0;
//            foreach( var t in LeadingTrivias )
//            {
//                if( !filter( t, idx++ ) ) break;
//                --keep;
//            }
//        }
//        if( keep == 0 )
//        {
//            IList<SqlNode> content = GetRawContent();
//            int idx;
//            SqlNode c = RawGetFirstChildInContent( content, out idx );
//            if( c != null )
//            {
//                content = RawReplaceContentNode( content, idx, c.ExtractLeadingTrivias( filter ) );
//            }
//            else
//            {
//                if( nb == 0 ) return this;
//                content = null;
//            }
//            return DoClone( ImmutableList<SqlTrivia>.Empty, content, TrailingTrivias );
//        }
//        else if( keep != nb )
//        {
//            return DoClone( LeadingTrivias.RemoveRange( 0, nb - keep ), null, TrailingTrivias );
//        }
//        return this;
//    }

//    internal SqlNode DoSetRawContent( IList<SqlNode> childrenNodes )
//    {
//        if( childrenNodes == null ) childrenNodes = Array.Empty<SqlNode>();
//        return DoClone( LeadingTrivias, childrenNodes, TrailingTrivias );
//    }

//    internal SqlNode DoReplaceContentNode( int i, SqlNode child )
//    {
//        var c = RawReplaceContentNode( GetRawContent(), i, child );
//        return c != null ? DoClone( LeadingTrivias, c, TrailingTrivias ) : this;
//    }

//    internal SqlNode DoReplaceContentNode( Func<SqlNode, int, int, SqlNode> replacer )
//    {
//        bool change = false;
//        var list = GetRawContent();
//        var pos = 0;
//        for( int i = 0; i < list.Count; ++i )
//        {
//            var current = list[i];
//            var replaced = replacer( current, pos, i );
//            if( replaced != null || list is SqlNode[] )
//            {
//                if( current != replaced )
//                {
//                    change = true;
//                    list[i] = replaced;
//                }
//            }
//            else
//            {
//                change = true;
//                list.RemoveAt( i-- );
//            }
//            if( current != null ) pos += current.Width;
//        }
//        return change ? DoClone( LeadingTrivias, list, TrailingTrivias ) : this;
//    }

//    internal SqlNode DoReplaceContentNode( int i1, SqlNode child1, int i2, SqlNode child2 )
//    {
//        var c = RawReplaceContentNode( GetRawContent(), i1, child1, i2, child2 );
//        return c != null ? DoClone( LeadingTrivias, c, TrailingTrivias ) : this;
//    }

//    internal SqlNode DoStuffRawContent( int iStart, int count, IReadOnlyList<SqlNode> children )
//    {
//        if( children == null ) throw new ArgumentNullException( nameof( children ) );
//        IList<SqlNode> c = GetRawContent();
//        RawStuffContent( c, iStart, count, children );
//        return DoClone( LeadingTrivias, c, TrailingTrivias );
//    }

//    static IList<SqlNode> RawReplaceContentNode( IList<SqlNode> content, int i, SqlNode child )
//    {
//        if( child != null || content is SqlNode[] )
//        {
//            if( content[i] == child ) return null;
//            content[i] = child;
//        }
//        else content.RemoveAt( i );
//        return content;
//    }

//    static IList<SqlNode> RawReplaceContentNode( IList<SqlNode> content, int i1, SqlNode child1, int i2, SqlNode child2 )
//    {
//        if( (child1 != null && child2 != null) || content is SqlNode[] )
//        {
//            if( content[i1] == child1 && content[i2] == child2 ) return null;
//            content[i1] = child1;
//            content[i2] = child2;
//        }
//        else
//        {
//            if( child1 == null )
//            {
//                content.RemoveAt( i1 );
//                if( i1 < i2 ) --i2;
//            }
//            else content[i1] = child1;

//            if( child2 == null ) content.RemoveAt( i2 );
//            else content[i2] = child2;
//        }
//        return content;
//    }

//    static SqlNode RawGetFirstChildInContent( IList<SqlNode> content, out int idx )
//    {
//        SqlNode firstChild = null;
//        for( idx = 0; idx < content.Count; ++idx )
//            if( (firstChild = content[idx]) != null ) break;
//        return firstChild;
//    }

//    static SqlNode RawGetLastChildInContent( IList<SqlNode> content, out int idx )
//    {
//        SqlNode lastChild = null;
//        for( idx = content.Count - 1; idx >= 0; --idx )
//            if( (lastChild = content[idx]) != null ) break;
//        return lastChild;
//    }

//    static IList<SqlNode> RawStuffContent( IList<SqlNode> content, int iStart, int count, IReadOnlyList<SqlNode> children )
//    {
//        List<SqlNode> lC = content as List<SqlNode>;
//        if( lC == null || children.Count == count )
//        {
//            Debug.Assert( lC == null || content is SqlNode[] );
//            bool changed = false;
//            for( int i = 0; i < count; ++i )
//            {
//                if( content[iStart + i] != children[i] )
//                {
//                    content[iStart + i] = children[i];
//                    changed = true;
//                }
//            }
//            return changed ? content : null;
//        }
//        if( lC == null ) throw new InvalidOperationException();
//        lC.RemoveRange( iStart, count );
//        lC.InsertRange( iStart, children );
//        return content;
//    }

//    internal SqlNode DoAddLeadingTrivia( SqlTrivia t, Func<SqlTrivia, bool> skipper )
//    {
//        if( t.IsEmpty ) return this;
//        int i = 0;
//        if( skipper != null )
//        {
//            foreach( var p in LeadingTrivias )
//            {
//                if( !skipper( p ) ) break;
//                ++i;
//            }
//        }
//        return DoClone( LeadingTrivias.Insert( i, t ), null, TrailingTrivias );
//    }

//    internal SqlNode DoAddTrailingTrivia( SqlTrivia t, Func<SqlTrivia, bool> skipper )
//    {
//        if( t.IsEmpty ) return this;
//        int count = TrailingTrivias.Count;
//        int idx = count;
//        if( skipper != null )
//        {
//            for( int i = 0; i < count; ++i )
//            {
//                if( !skipper( TrailingTrivias[idx - 1] ) ) break;
//                --idx;
//            }
//        }
//        return DoClone( LeadingTrivias, null, idx == count ? TrailingTrivias.Add( t ) : TrailingTrivias.Insert( idx, t ) );
//    }

//    public virtual SqlNode UnPar => this;

//    /// <summary>
//    /// Fundamental method that rebuilds this node with new trivias and content.
//    /// </summary>
//    /// <param name="leading">Leading trivias.</param>
//    /// <param name="content">New content.</param>
//    /// <param name="trailing">Trailing trivias.</param>
//    /// <returns>A new immutable object.</returns>
//    protected abstract SqlNode DoClone( ImmutableList<SqlTrivia> leading, IList<SqlNode> content, ImmutableList<SqlTrivia> trailing );

//    /// <summary>
//    /// Required because of SqlExternalNode: DoClone can not be internal protected.
//    /// </summary>
//    internal SqlNode InternalDoClone( ImmutableList<SqlTrivia> leading, IList<SqlNode> content, ImmutableList<SqlTrivia> trailing )
//    {
//        return leading == LeadingTrivias && content == null && trailing == TrailingTrivias
//                ? this
//                : DoClone( leading, content, trailing );
//    }

//    internal protected abstract SqlNode Accept( SqlNodeVisitor visitor );

//    public void Write( ISqlTextWriter w )
//    {
//        foreach( var t in LeadingTrivias ) w.Write( t );
//        WriteWithoutTrivias( w );
//        foreach( var t in TrailingTrivias ) w.Write( t );
//    }

//    public virtual void WriteWithoutTrivias( ISqlTextWriter w )
//    {
//        foreach( var t in ChildrenNodes ) t.Write( w );
//    }

//    /// <summary>
//    /// Overridden to return a compact representation on one line 
//    /// without trivias (see <see cref="SqlTextWriter.CreateOneLineCompact"/>).
//    /// </summary>
//    /// <returns>One line, compact, representation.</returns>
//    public override string ToString()
//    {
//        ISqlTextWriter w = SqlTextWriter.CreateOneLineCompact();
//        WriteWithoutTrivias( w );
//        return w.ToString();
//    }

//    public string ToString( bool withThisTrivia, bool restoreUselessComments = false )
//    {
//        ISqlTextWriter w = SqlTextWriter.CreateDefault( new StringBuilder(), restoreUselessComments );
//        if( withThisTrivia ) Write( w );
//        else WriteWithoutTrivias( w );
//        return w.ToString();
//    }

//}
