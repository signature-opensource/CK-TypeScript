using CK.Core;
using CK.Transform.Core;
using System.Diagnostics;

namespace CK.Transform.TransformLanguage;


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
