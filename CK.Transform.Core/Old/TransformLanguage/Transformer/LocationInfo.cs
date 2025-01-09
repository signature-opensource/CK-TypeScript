using CK.Transform.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Captures operational information to target one or more nodes.
/// Cardinality check is handled thanks to <see cref="LocationCardinalityInfo"/>.
/// <para>
/// Nodes are selected based on a <see cref="TriviaMatcher"/> or a <see cref="NodeMatcher"/> predicate.
/// <see cref="LocationInserter"/> uses this to find one or more <see cref="LocationInserter.MatchedNode"/>
/// that are used by transform visitors like <see cref="TriviaInjectionPointVisitor"/>.
/// </para>
/// </summary>
public readonly struct LocationInfo
{
    /// <summary>
    /// The trivia matcher is not null if and only if <see cref="NodeMatcher"/> is null.
    /// </summary>
    public readonly Func<Trivia, bool>? TriviaMatcher;

    /// <summary>
    /// Node matcher is not null if and only if <see cref="TriviaMatcher"/> is null.
    /// </summary>
    public readonly Func<AbstractNode, bool>? NodeMatcher;

    /// <summary>
    /// The cardinality specification.
    /// </summary>
    public readonly LocationCardinalityInfo Card;

    internal LocationInfo( TriviaInjectionPointMatcher m )
    {
        // Single cardinality for a <InjectionPoint>.
        Card = new LocationCardinalityInfo();
        TriviaMatcher = m.Match;
    }

    internal string GetDescription()
    {
        // This is not cached (readonly struct). This is used only
        // for error details (and in debug by this ToString).
        if( TriviaMatcher != null )
        {
            return $"injection point '{((TriviaInjectionPointMatcher)TriviaMatcher.Target!).InjectionPoint.Text}'";
        }
        throw new NotImplementedException();
    }

    public override string ToString() => Card.ToString() + " " + GetDescription();
}
