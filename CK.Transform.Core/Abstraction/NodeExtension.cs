using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.Core;

public static class NodeExtension
{
    /// <summary>
    /// Gets a flattened list of <see cref="TokenNode"/>.
    /// </summary>
    /// <param name="nodes">This enumerable of Node.</param>
    /// <returns>The flattened list of tokens.</returns>
    static public IEnumerable<TokenNode> ToTokens( this IEnumerable<AbstractNode> nodes )
    {
        foreach( var a in nodes )
        {
            if( a is TokenNode t ) yield return t;
            else foreach( var ta in ToTokens( a.AllTokens ) ) yield return ta;
        }
    }

    /// <summary>
    /// Sets trivias around this node.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="leading">Leading trivia. Can be null for empty trivias.</param>
    /// <param name="trailing">Trailing trivia. Can be null for empty trivias.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T SetTrivias<T>( this T @this, ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing ) where T : AbstractNode
    {
        return (T)@this.DoSetTrivias( leading, trailing );
    }

    /// <summary>
    /// Adds a leading trivia.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="t">The trivia to add in front.</param>
    /// <param name="skipper">Optional skipper predicate.</param>
    /// <returns>A new immutable object.</returns>
    static public T AddLeadingTrivia<T>( this T @this, Trivia t, Func<Trivia, bool>? skipper = null ) where T : AbstractNode
    {
        return (T)@this.DoAddLeadingTrivia( t, skipper );
    }

    /// <summary>
    /// Adds a trailing trivia.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="t">The trivia to append.</param>
    /// <param name="skipper">Optional function to skip trivias.</param>
    /// <returns>A new immutable object.</returns>
    static public T AddTrailingTrivia<T>( this T @this, Trivia t, Func<Trivia, bool>? skipper = null ) where T : AbstractNode
    {
        return (T)@this.DoAddTrailingTrivia( t, skipper );
    }

    /// <summary>
    /// Removes leading trivias (in <see cref="AbstractNode.FullLeadingTrivias"/>) from left to right that match
    /// the predicate. Extraction ends as soon as the predicate returns false.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="predicate">The predicate.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T ExtractLeadingTrivias<T>( this T @this, Func<Trivia, int, bool> predicate ) where T : AbstractNode
    {
        return (T)@this.DoExtractLeadingTrivias( predicate );
    }

    /// <summary>
    /// Removes trailing trivias (in <see cref="AbstractNode.FullTrailingTrivias"/>) from right to end that match
    /// the predicate. Extraction ends as soon as the predicate returns false.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="predicate">The predicate.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T ExtractTrailingTrivias<T>( this T @this, Func<Trivia, int, bool> predicate ) where T : AbstractNode
    {
        return (T)@this.DoExtractTrailingTrivias( predicate );
    }

    /// <summary>
    /// Lifts leading and trailing trivias: <see cref="AbstractNode.TrailingNodes"/> and <see cref="AbstractNode.LeadingNodes"/> do not 
    /// have trailing trivias any more.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T LiftBothTrivias<T>( this T @this ) where T : AbstractNode
    {
        return (T)@this.DoLiftBothTrivias();
    }


    /// <summary>
    /// Lifts leading trivias: <see cref="AbstractNode.LeadingNodes"/> do not have leading trivias any more.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T LiftLeadingTrivias<T>( this T @this ) where T : AbstractNode
    {
        return (T)@this.DoLiftLeadingTrivias();
    }

    /// <summary>
    /// Lifts trailing trivias: <see cref="AbstractNode.TrailingNodes"/> do not have trailing trivias any more.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T LiftTrailingTrivias<T>( this T @this ) where T : AbstractNode
    {
        return (T)@this.DoLiftTrailingTrivias();
    }

    /// <summary>
    /// Updates or removes/clears one or more children in children (see <see cref="AbstractNode.GetRawContent"/>).
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="replacer">
    /// Mapping function. Must return null to remove or clear the node.
    /// The first parameter is the relative position of the child
    /// node (the sum of the previous siblings <see cref="AbstractNode.Width"/>),
    /// the second integer is the raw index in the <see cref="AbstractNode.GetRawContent"/> list.
    /// </param>
    /// <returns>A new immutable object or this node if no change occurred.</returns>
    static public T ReplaceContentNode<T>( this T @this, Func<AbstractNode, int, int, AbstractNode> replacer ) where T : AbstractNode
    {
        return (T)@this.DoReplaceContentNode( replacer );
    }

    /// <summary>
    /// Sets or removes/clears a child at a given index in raw children (see <see cref="AbstractNode.GetRawContent"/>).
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="i">The index that must be replaced.</param>
    /// <param name="child">The replacement. Null to remove or clear the node.</param>
    /// <returns>A new immutable object or this node if no change occurred.</returns>
    static public T ReplaceContentNode<T>( this T @this, int i, AbstractNode child ) where T : AbstractNode
    {
        return (T)@this.DoReplaceContentNode( i, child );
    }

    /// <summary>
    /// Sets or removes/clears two children at given indexes in raw children (see <see cref="AbstractNode.GetRawContent"/>).
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="i1">The first index that must be replaced.</param>
    /// <param name="child1">The first replacement. Null to remove or clear the node.</param>
    /// <param name="i2">The first index that must be replaced.</param>
    /// <param name="child2">The first replacement. Null to remove or clear the node.</param>
    /// <returns>A new immutable object or this node if no change occurred.</returns>
    static public T ReplaceContentNode<T>( this T @this, int i1, AbstractNode child1, int i2, AbstractNode child2 ) where T : AbstractNode
    {
        return (T)@this.DoReplaceContentNode( i1, child1, i2, child2 );
    }

    /// <summary>
    /// Sets new children nodes.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="childrenNodes">Children nodes.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T SetRawContent<T>( this T @this, IList<AbstractNode> childrenNodes ) where T : AbstractNode
    {
        return (T)@this.DoSetRawContent( childrenNodes );
    }

    /// <summary>
    /// Inserts or replace one or more children at a given index in <see cref="AbstractNode.GetRawContent"/>.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="iStart">The index.</param>
    /// <param name="count">The number of children to replace.</param>
    /// <param name="children">The children to insert.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T StuffRawContent<T>( this T @this, int iStart, int count, IReadOnlyList<AbstractNode> children ) where T : AbstractNode
    {
        return (T)@this.DoStuffRawContent( iStart, count, children );
    }

    /// <summary>
    /// Inserts or replace one or more children at a given index in <see cref="AbstractNode.GetRawContent"/>.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="iStart">The index.</param>
    /// <param name="count">The number of children to replace.</param>
    /// <param name="children">The children to insert.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T StuffRawContent<T>( this T @this, int iStart, int count, params AbstractNode[] children ) where T : AbstractNode
    {
        return (T)@this.DoStuffRawContent( iStart, count, children );
    }

}
