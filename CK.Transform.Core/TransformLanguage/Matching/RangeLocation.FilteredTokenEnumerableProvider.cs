using System.Collections.Generic;
using System;
using CK.Core;
using System.Text;
using System.Linq;

namespace CK.Transform.Core;

public sealed partial class RangeLocation : IFilteredTokenEnumerableProvider
{
    public void Activate( Action<IFilteredTokenEnumerableProvider> collector )
    {
        Throw.DebugAssert( CheckValid() );
        if( IsBetween )
        {
            Second.Activate( collector );
            collector( new Before() );
            First.Activate( collector );
            collector( new After() );
        }
        else if( IsAfter )
        {
            First.Activate( collector );
            collector( new After() );
        }
        else
        {
            First.Activate( collector );
            collector( new Before() );
        }
    }

    Func<ITokenFilterBuilderContext,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> IFilteredTokenEnumerableProvider.GetFilteredTokenProjection()
    {
        return IFilteredTokenEnumerableProvider.ThrowOnCombinedProvider();
    }

    sealed class Before : IFilteredTokenEnumerableProvider
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
            Throw.DebugAssert( "We are a 'Before' context.", !context.IsRoot );
            // We need to retrieve the tokens from the PREVIOUS inner, the source inner of the mono-location LocationMatcher
            // that is our previous.
            Throw.DebugAssert( "We are working on the result of a mono-location LocationCardinality.",
                               context.Previous.Tokens == inner
                               && (context.Previous.Provider == LocationCardinality.SingleCardinality
                                   || (context.Previous.Provider is LocationCardinality card
                                       && card.Kind is LocationCardinality.LocationKind.Single
                                                       or LocationCardinality.LocationKind.First
                                                       or LocationCardinality.LocationKind.Last) ) );
            var previousInput = context.Previous.Previous;
            do
            {
                Throw.DebugAssert( "A LocationCardinality is followed by a matcher and the root is a SyntaxBorder.", !previousInput.IsRoot );
                previousInput = previousInput.Previous;
            }
            while( !previousInput.IsSyntaxBorder );

            var spans = context.CreateDynamicSpan();

            // Captures (for each group), the start of the location.
            foreach( var each in inner )
            {
                Throw.DebugAssert( "Previous filter is a mono-location LocationMatcher.", each.Count() == 1 );
                int futureEnd = each.First().First().Index;
                spans.AppendSpan( new TokenSpan( futureEnd, futureEnd + 1 ) );
            }
            // Then detect the start of the LocationMatcher input and if the resulting
            // range is not empty, keep the span as a "dynamic span".
            int idx = 0;
            var previousInner = previousInput.Tokens;
            foreach( var each in previousInner )
            {
                int beg = each.First().First().Index;
                int end = spans.SpanAt( idx ).Beg;
                Throw.DebugAssert( beg <= end );
                if( beg == end )
                {
                    spans.RemoveAt( idx );
                }
                else
                {
                    spans.SetSpanAt( idx++, new TokenSpan( beg, end ) );
                }
            }
            return spans.LockEachGroups();
        }

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "before" );

        public override string ToString() => "before";
    }

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
            Throw.DebugAssert( "We are an 'After' context.", !context.IsRoot );
            // We need to retrieve the tokens from the PREVIOUS inner, the source inner of the mono-location LocationMatcher
            // or the Before (when we are in a between) that is our previous.
            Throw.DebugAssert( "We are working on the result of a mono-location LocationCardinality or a Before.",
                               context.Previous.Tokens == inner
                               && (context.Previous.Provider is Before
                                   || context.Previous.Provider == LocationCardinality.SingleCardinality
                                   || (context.Previous.Provider is LocationCardinality card
                                       && card.Kind is LocationCardinality.LocationKind.Single
                                                    or LocationCardinality.LocationKind.First
                                                    or LocationCardinality.LocationKind.Last)) );
            // If we are after a Before, the Before is our previous inner (not the inner of the between that activated
            // the Before and this After).
            var previousInput = context.Previous.Previous;
            Throw.DebugAssert( previousInput != null );
            if( context.Previous.Provider is not Before )
            {
                do
                {
                    Throw.DebugAssert( "A LocationCardinality is followed by a matcher and the root is a SyntaxBorder.",
                                       !previousInput.IsRoot );
                    previousInput = previousInput.Previous;
                }
                while( !previousInput.IsSyntaxBorder );
            }

            Throw.DebugAssert( "A LocationMatcher or a Before is not the root.", !context.Previous.IsRoot );

            var spans = context.CreateDynamicSpan();
            foreach( var each in inner )
            {
                Throw.DebugAssert( "Previous filter is a mono-location LocationMatcher or a Before.", each.Count() == 1 );
                // Declares the [end,end+1[ range so that when setting the final span below
                // to not fail because of a 1-token overlap.
                int end = each.First().Last().Index + 1;
                spans.AppendSpan( new TokenSpan( end, end + 1 ) );
            }
            // Then detect the end of the LocationMatcher input and if the resulting
            // range is not empty, keep the span as a "dynamic span".
            int idx = 0;
            var previousInner = previousInput.Tokens;
            foreach( var each in previousInner )
            {
                int beg = spans.SpanAt(idx).Beg;
                int end = each.Last().Last().Index;
                Throw.DebugAssert( beg <= end );
                if( beg == end )
                {
                    spans.RemoveAt( idx );
                }
                else
                {
                    spans.SetSpanAt( idx++, new TokenSpan( beg, end ) );
                }
            }
            return spans.LockEachGroups();
        }

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "after" );

        public override string ToString() => "after";
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
