using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Transform.Core;

public sealed partial class LocationMatcher : IFilteredTokenEnumerableProvider
{
    /// <summary>
    /// Activates the <see cref="Matcher"/> and then the non null <see cref="LocationCardinality"/> or
    /// the <see cref="LocationCardinality.SingleCardinality"/>.
    /// </summary>
    /// <param name="collector">The provider collector.</param>
    public void Activate( Action<IFilteredTokenEnumerableProvider> collector )
    {
        Throw.DebugAssert( CheckValid() );
        Matcher.Activate( collector );
        (Cardinality ?? LocationCardinality.SingleCardinality).Activate( collector );
    }

    public Func<ITokenFilterBuilderContext,
                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection()
    {
        return IFilteredTokenEnumerableProvider.ThrowOnCombinedProvider();
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "LocationMatcher[ " );
        if( !CheckValid() ) b.Append( "<Invalid>" );
        else
        {
            if( Cardinality != null ) Cardinality.Describe( b, parsable );
            else b.Append( "single" );
            Matcher.Describe( b, parsable );
        }
        if( !parsable ) b.Append( " ]" );
        return b;
    }

    public override string ToString() => Describe( new StringBuilder(), parsable: true ).ToString();
}
