using CK.Core;
using CK.Transform.Core;
using System;
using System.Diagnostics;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Captures operational information from a <see cref="ISqlTLocationFinder"/>.
/// Cardinality check is handled thanks to <see cref="LocationCardinalityInfo"/>.
/// This handles the 5 kind of matches: part and statement, range match, trivia match 
/// and fragment match.
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


sealed class TriviaInjectionPointVisitor : TransformVisitor
{
    readonly InjectIntoStatement _injectInto;
    readonly TriviaInjectionPointMatcher _matcher;
    readonly LocationInserter _inserter;

    internal TriviaInjectionPointVisitor( IActivityMonitor monitor, InjectIntoStatement injecter )
        : base( monitor )
    {
        _injectInto = injecter;
        _matcher = new TriviaInjectionPointMatcher( monitor, injecter.Target, injecter.Content );
        _inserter = new LocationInserter( new LocationInfo( _matcher ) );
    }

    protected override bool BeforeVisitItem() => true;

    protected override AbstractNode AfterVisitItem( AbstractNode e )
    {
        Debug.Assert( !_inserter.CanStop );
        var m = _inserter.AddCandidate( Monitor, VisitContext.Position, e );
        if( m != null )
        {
            e = m.Apply( Monitor, _matcher.TextBefore, _matcher.TextAfter, false, _matcher.TextReplace );
            Debug.Assert( !_inserter.CanStop );
            SetHasUnParsedText();
        }
        if( VisitContext.Depth == 0 && _inserter.MatchCount == 0 )
        {
            Monitor.Error( $"Unable to find injection point '<{_injectInto.Target.Name}/>'." );
        }
        return e;
    }
}
