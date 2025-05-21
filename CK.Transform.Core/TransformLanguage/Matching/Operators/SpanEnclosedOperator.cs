using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// A <see cref="ITokenFilterOperator"/> that finds the deepest
/// spans defined by opening/closing pair of tokens.
/// This is like the <see cref="SpanTypeOperator"/> but "dynamic" as no
/// <see cref="SourceSpan"/> are required to exist.
/// </summary>
public sealed class SpanEnclosedOperator : ITokenFilterOperator
{
    readonly string _openingToken;
    readonly string _closingToken;

    public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

    public SpanEnclosedOperator( string openingToken, string closingToken )
    {
        _openingToken = openingToken;
        _closingToken = closingToken;
    }

    public void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        var builder = context.SharedBuilder;
        var e = input.CreateTokenEnumerator();
        while( e.NextEach() )
        {
            while( e.NextMatch() )
            {
                while( e.NextToken() )
                {

                }
            }
        }
        context.SetResult( builder );

    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {

        throw new NotImplementedException();
    }

    public override string ToString() => Describe( new StringBuilder(), true ).ToString();
}
