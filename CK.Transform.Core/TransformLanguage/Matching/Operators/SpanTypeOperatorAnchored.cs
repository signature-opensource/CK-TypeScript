using CK.Core;
using System;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// The anchored version of the <see cref="SpanTypeOperator"/>:
/// the tokens are the ones from the where "pattern" but the span scope used
/// is the corresponding match in the previous input.
/// </summary>
sealed class SpanTypeOperatorAnchored : ITokenFilterOperator
{
    readonly Type _spanType;
    readonly string _displayName;

    public SpanTypeOperatorAnchored( Type spanType, string displayName )
    {
        _spanType = spanType;
        _displayName = displayName;
    }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName => _displayName;

    /// <summary>
    /// Collects this operator.
    /// </summary>
    /// <param name="collector">The operator collector.</param>
    public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

    public void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        Throw.DebugAssert( "Our previous is the where TokenPatternOperator.", input.Previous != null );
        var builder = context.SharedBuilder;
        var spanCollector = new TokenSpanDeepestCollector();
        var ePrevious = input.Previous.CreateTokenEnumerator();
        var e = input.CreateTokenEnumerator();
        while( e.NextEach() )
        {
            ePrevious.NextEach();
            Throw.DebugAssert( e.CurrentEachIndex == ePrevious.CurrentEachIndex );
            while( e.NextMatch() )
            {
                if( ePrevious.State is TokenFilterEnumeratorState.Each
                    || ePrevious.CurrentMatch.Span.End < e.CurrentMatch.Span.Beg )
                {
                    ePrevious.NextMatch();
                }
                Throw.DebugAssert( ePrevious.CurrentMatch.Span.ContainsOrEquals( e.CurrentMatch.Span ) );
                while( e.NextToken() )
                {
                    var s = context.GetDeepestSpanAt( e.Token.Index, _spanType );
                    if( s != null && ePrevious.CurrentMatch.Span.Contains( s.Span ) )
                    {
                        spanCollector.Add( s.Span );
                    }
                }
            }
            if( !spanCollector.ExtractResultToNewEach( builder ) )
            {
                context.SetFailedResult( "Missing span.", e );
                return;
            }
        }
        context.SetResult( builder );
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "[AnchoredCoveringSpanType] " );
        return b.Append( _displayName );
    }

    public override string ToString() => _displayName;


}

