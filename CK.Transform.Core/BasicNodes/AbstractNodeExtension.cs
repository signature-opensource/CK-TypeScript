using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace CK.Transform.Core;

public static class AbstractNodeExtension
{
    /// <summary>
    /// Gets a flattened list of <see cref="TokenNode"/>.
    /// </summary>
    /// <param name="nodes">This enumerable of node.</param>
    /// <returns>The flattened list of tokens.</returns>
    static public IEnumerable<TokenNode> ToTokens( this IEnumerable<IAbstractNode> nodes )
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
    static public T SetTrivias<T>( this T @this, ImmutableArray<Trivia> leading, ImmutableArray<Trivia> trailing ) where T : class, IAbstractNode
    {
        return Unsafe.As<T>( Unsafe.As<AbstractNode>( @this ).CloneForTrivias( leading, trailing ) );
    }

    /// <summary>
    /// Adds a leading trivia.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="t">The trivia to add in front.</param>
    /// <param name="skipper">Optional skipper predicate.</param>
    /// <returns>A new immutable object.</returns>
    static public T AddLeadingTrivia<T>( this T @this, Trivia t, Func<Trivia, bool>? skipper = null ) where T : class, IAbstractNode
    {
        return Unsafe.As<T>( Unsafe.As<AbstractNode>( @this ).DoAddLeadingTrivia( t, skipper ) );
    }

    /// <summary>
    /// Adds a trailing trivia.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="t">The trivia to append.</param>
    /// <param name="skipper">Optional function to skip trivias.</param>
    /// <returns>A new immutable object.</returns>
    static public T AddTrailingTrivia<T>( this T @this, Trivia t, Func<Trivia, bool>? skipper = null ) where T : class, IAbstractNode
    {
        return Unsafe.As<T>( Unsafe.As<AbstractNode>( @this ).DoAddTrailingTrivia( t, skipper ) );
    }

    /// <summary>
    /// Removes leading trivias (in <see cref="AbstractNode.FullLeadingTrivias"/>) from left to right that match
    /// the predicate. Extraction ends as soon as the predicate returns false.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="predicate">The predicate.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T ExtractLeadingTrivias<T>( this T @this, Func<Trivia, int, bool> predicate ) where T : class, IAbstractNode
    {
        return Unsafe.As<T>( Unsafe.As<AbstractNode>( @this ).DoExtractLeadingTrivias( predicate ) );
    }

    /// <summary>
    /// Removes trailing trivias (in <see cref="AbstractNode.FullTrailingTrivias"/>) from right to end that match
    /// the predicate. Extraction ends as soon as the predicate returns false.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <param name="predicate">The predicate.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T ExtractTrailingTrivias<T>( this T @this, Func<Trivia, int, bool> predicate ) where T : class, IAbstractNode
    {
        return Unsafe.As<T>( Unsafe.As<AbstractNode>( @this ).DoExtractTrailingTrivias( predicate ) );
    }

    /// <summary>
    /// Lifts leading and trailing trivias: <see cref="AbstractNode.TrailingNodes"/> and <see cref="AbstractNode.LeadingNodes"/> do not 
    /// have trailing trivias any more.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T LiftBothTrivias<T>( this T @this ) where T : class, IAbstractNode
    {
        return Unsafe.As<T>( Unsafe.As<AbstractNode>( @this ).DoLiftBothTrivias() );
    }


    /// <summary>
    /// Lifts leading trivias: <see cref="AbstractNode.LeadingNodes"/> do not have leading trivias any more.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T LiftLeadingTrivias<T>( this T @this ) where T : class, IAbstractNode
    {
        return Unsafe.As<T>( Unsafe.As<AbstractNode>( @this ).DoLiftLeadingTrivias() );
    }

    /// <summary>
    /// Lifts trailing trivias: <see cref="AbstractNode.TrailingNodes"/> do not have trailing trivias any more.
    /// </summary>
    /// <param name="this">This node.</param>
    /// <returns>A new immutable object or this if no change occurred.</returns>
    static public T LiftTrailingTrivias<T>( this T @this ) where T : class, IAbstractNode
    {
        return Unsafe.As<T>( Unsafe.As<AbstractNode>( @this ).DoLiftTrailingTrivias() );
    }

}
