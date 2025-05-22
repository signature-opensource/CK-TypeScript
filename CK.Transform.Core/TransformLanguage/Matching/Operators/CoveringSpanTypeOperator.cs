using System;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="ITokenFilterOperator"/> that splits matches to
/// the top spans that can be assigned to a type.
/// </summary>
public sealed class CoveringSpanTypeOperator : ITokenFilterOperator
{
    readonly Type _spanType;
    readonly string _displayName;

    /// <summary>
    /// Initializes a new <see cref="CoveringSpanTypeOperator"/>.
    /// </summary>
    /// <param name="displayName">The span type name to display.</param>
    /// <param name="spanType">The span type to consider.</param>
    public CoveringSpanTypeOperator( string displayName, Type spanType )
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
        var spanCollector = new TokenSpanCoveringCollector();
        var e = input.CreateTokenEnumerator();
        while( e.NextEach( skipEmpty: false ) )
        {
            while( e.NextMatch() )
            {
                // We should work at the match's span level here.
                // This is currently far from optimal but it defines
                // the right behavior. Any optimization should produce
                // the same results.
                while( e.NextToken() )
                {
                    var s = context.GetTopSpanAt( e.Token.Index, _spanType, e.CurrentMatch.Span );
                    if( s != null ) spanCollector.Add( s.Span );
                }
                // We can free the current collector list as
                // the spans between matches are independent.
                spanCollector.ExtractSpansTo( builder );
            }
            builder.StartNewEach( skipEmpty: false );
        }
        context.SetResult( builder );
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "[CoveringSpanType] " );
        return b.Append( _displayName );
    }

    public override string ToString() => _displayName;


}

