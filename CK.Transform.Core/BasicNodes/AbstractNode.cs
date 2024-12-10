using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Fundamental Abstract Syntax Tree item, base class of all possible <see cref="TokenNode"/>
/// and <see cref="SyntaxNode"/>.
/// </summary>
public abstract partial class AbstractNode : IAbstractNode
{
    // Not readonly for the CloneWithTrivias that uses MemberwiseClone so
    // no extra virtual/override is required.
    ImmutableArray<Trivia> _leadingTrivias;
    ImmutableArray<Trivia> _trailingTrivias;

    private protected AbstractNode( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing )
    {
        Throw.DebugAssert( leading.IsDefault is false && trailing.IsDefault is false );
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
    public abstract NodeType NodeType { get; }

    /// <summary>
    /// Creates a mutator object that can <see cref="AbstractNodeMutator.Clone()"/> this node.
    /// </summary>
    /// <returns>A mutator for this node.</returns>
    public abstract AbstractNodeMutator CreateMutator();

    /// <summary>
    /// Always throw an <see cref="ArgumentException"/> for any invalid content in this node
    /// even if <see cref="DoCheckInvariants()"/> (that is the actual implementation) throws
    /// another type of exception.
    /// </summary>
    public void CheckInvariants()
    {
        try
        {
            DoCheckInvariants();
        }
        catch( Exception ex ) when( ex is not ArgumentException ) 
        {
            throw new ArgumentException( "Invalid node content.", ex );
        }
    }

    /// <summary>
    /// Should throw <see cref="ArgumentException"/> for any invalid content in this node.
    /// <para>
    /// Other type of exception can be thrown (typically an <see cref="IndexOutOfRangeException"/> from
    /// a <see cref="CompositeNode"/>): they will be wrapped in an <see cref="ArgumentException"/> by
    /// the public <see cref="CheckInvariants()"/> entry point.
    /// </para>
    /// </summary>
    protected abstract void DoCheckInvariants();

    /// <summary>
    /// Finds the token at a relative position in this node. if the index is out of 
    /// range (ie. negative or greater or equal to <see cref="Width"/>), null is returned.
    /// </summary>
    /// <param name="index">The zero based index of the token to locate.</param>
    /// <param name="onPath">Will be called for each intermediate node with the relative index of its first token.</param>
    /// <returns>The token or null if index is out of range.</returns>
    public TokenNode? LocateToken( int index, Action<AbstractNode, int> onPath )
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

    internal AbstractNode CloneForTrivias( ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing )
    {
        var c = Unsafe.As<AbstractNode>( MemberwiseClone() );
        c._leadingTrivias = leading;
        c._trailingTrivias = trailing;
        return c;
    }

    internal AbstractNode DoLiftLeadingTrivias() => DoLift( ImmutableArray.CreateBuilder<Trivia>(), null, this, true );

    internal AbstractNode DoLiftTrailingTrivias() => DoLift( null, ImmutableArray.CreateBuilder<Trivia>(), this, true );

    internal AbstractNode DoLiftBothTrivias() => DoLift( ImmutableArray.CreateBuilder<Trivia>(), ImmutableArray.CreateBuilder<Trivia>(), this, true );

    static AbstractNode DoLift( ImmutableArray<Trivia>.Builder? hL, ImmutableArray<Trivia>.Builder? tL, AbstractNode n, bool root )
    {
        hL?.AddRange( n.LeadingTrivias );
        var mutator = n.CreateMutator();
        int nbC = n.ChildrenNodes.Count;
        if( nbC > 0 )
        {
            if( nbC == 1 || hL != null )
            {
                var firstChild = mutator.GetFirstChildForTrivia( out var idx );
                Throw.DebugAssert( firstChild != null );
                mutator.ReplaceForTrivia( idx, DoLift( hL, nbC == 1 ? tL : null, firstChild, false ) );
            }
            if( nbC > 1 && tL != null )
            {
                var lastChild = mutator.GetLastChildForTrivia( out var idx );
                Throw.DebugAssert( lastChild != null );
                mutator.ReplaceForTrivia( idx, DoLift( null, tL, lastChild, false ) );
            }
        }
        if( root )
        {
            if( hL != null ) mutator.Leading = hL.DrainToImmutable();
            else
            {
                Throw.DebugAssert( tL != null );
                tL.AddRange( n._trailingTrivias );
                mutator.Trailing = tL.DrainToImmutable();
            }
        }
        else
        {
            if( hL != null ) mutator.Leading = ImmutableArray<Trivia>.Empty;
            else
            {
                Throw.DebugAssert( tL != null );
                tL.AddRange( n._trailingTrivias );
                mutator.Trailing = ImmutableArray<Trivia>.Empty;
            }
        }
        return mutator.Clone();
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
            var mutator = CreateMutator();
            AbstractNode? c = mutator.GetLastChildForTrivia( out int idx );
            if( c != null )
            {
                mutator.ReplaceForTrivia( idx, c.DoExtractTrailingTrivias( predicate ) );
            }
            else
            {
                if( nb == 0 ) return this;
            }
            mutator.Trailing = ImmutableArray<Trivia>.Empty;
            return mutator.Clone();
        }
        else if( keep != nb )
        {
            return CloneForTrivias( LeadingTrivias, TrailingTrivias.RemoveRange( nb - keep, keep ) );
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
            var mutator = CreateMutator();
            AbstractNode? c = mutator.GetFirstChildForTrivia( out int idx );
            if( c != null )
            {
                mutator.ReplaceForTrivia( idx, c.DoExtractLeadingTrivias( filter ) );
            }
            else
            {
                if( nb == 0 ) return this;
            }
            mutator.Leading = ImmutableArray<Trivia>.Empty;
            return mutator.Clone();
        }
        else if( keep != nb )
        {
            return CloneForTrivias( LeadingTrivias.RemoveRange( 0, nb - keep ), TrailingTrivias );
        }
        return this;
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
        return CloneForTrivias( LeadingTrivias.Insert( i, t ), TrailingTrivias );
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
        return CloneForTrivias( _leadingTrivias, _trailingTrivias.Insert( idx, t ) );
    }

    public StringBuilder Write( StringBuilder b )
    {
        foreach( var t in LeadingTrivias ) b.Append( t.Content );
        WriteWithoutTrivias( b );
        foreach( var t in TrailingTrivias ) b.Append( t.Content );
        return b;
    }

    protected virtual void WriteWithoutTrivias( StringBuilder b )
    {
        foreach( var t in ChildrenNodes ) t.Write( b );
    }

    public override string ToString() => Write( new StringBuilder() ).ToString();   
}
