using System.Collections.Generic;
using System;
using CK.Core;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.Transform.Core;

public sealed partial class RangeLocation : IFilteredTokenEnumerableProvider
{
    public void Activate( Action<IFilteredTokenEnumerableProvider> collector ) => collector( this );

    Func<ITokenFilterBuilderContext,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> IFilteredTokenEnumerableProvider.GetFilteredTokenProjection()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// After always applies after First has done its job.
    /// For each matched ranges, it returns a different range that starts at the end of the matched range
    /// and ends at the end of the range that was the inner of First... 
    /// </summary>
    sealed class After : IFilteredTokenEnumerableProvider
    {
        public void Activate( Action<IFilteredTokenEnumerableProvider> collector ) => collector( this );

        public Func<ITokenFilterBuilderContext,
                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> GetFilteredTokenProjection()
        {
            return Run;
        }

        IEnumerable<IEnumerable<IEnumerable<SourceToken>>> Run( ITokenFilterBuilderContext context,
                                                                IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
        {
            // We need to retrieve the tokens from the PREVIOUS inner, the one that is the input of the
            // First. 
            throw new NotImplementedException();
        }

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "after" );
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "RangeLocation[ " );
        if( !CheckValid() )
        {
            b.Append( _kind.Text.Span ).Append( "<Invalid>" );
        }
        else
        {
            if( IsBetween )
            {
                b.Append( "between " );
                First.Describe( b, parsable );
                b.Append( b );
                return Second.Describe( b, parsable );
            }
            b.Append( _kind.Text.Span );
            First.Describe( b, parsable );
        }
        if( !parsable ) b.Append( " ]" );
        return b;
    }

    public override string ToString() => Describe( new StringBuilder(), parsable: true ).ToString();
}
