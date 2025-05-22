using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="ITokenFilterOperator"/> that splits matches to
/// the deepest spans that can be assigned to a type.
/// <para>
/// Narrowing and splitter operator.
/// </para>
/// </summary>
public sealed class SpanTypeOperator : ITokenFilterOperator, ITokenFilterAnchoredOperator
{
    readonly Type _spanType;
    readonly string _displayName;

    /// <summary>
    /// Initializes a new <see cref="SpanTypeOperator"/>.
    /// </summary>
    /// <param name="spanType">The span type to consider.</param>
    /// <param name="displayName">The span type name to display.</param>
    public SpanTypeOperator( Type spanType, string displayName )
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
        var builder = context.SharedBuilder;
        var spanCollector = new TokenSpanDeepestCollector();
        var e = input.CreateTokenEnumerator();
        while( e.NextEach() )
        {
            while( e.NextMatch() )
            {
                // We should work at the match's span level here.
                // This is currently far from optimal but it defines
                // the right behavior. Any optimization should produce
                // the same results.
                while( e.NextToken() )
                {
                    var s = context.GetDeepestSpanAt( e.Token.Index, _spanType );
                    if( s != null && e.CurrentMatch.Span.Contains( s.Span ) )
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

    public ITokenFilterOperator ToAnchoredOperator()
    {
        return new SpanTypeOperatorAnchored( _spanType, _displayName );
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "[SpanType] " );
        return b.Append( _displayName );
    }

    public override string ToString() => _displayName;

}

