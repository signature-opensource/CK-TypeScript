using System.Collections.Generic;
using System;
using CK.Core;
using System.Text;
using System.Linq;
using System.Reflection;

namespace CK.Transform.Core;

public sealed partial class RangeLocation : IFilteredTokenOperator
{
    public void Activate( Action<IFilteredTokenOperator> collector )
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

    void IFilteredTokenOperator.Apply( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
    {
        IFilteredTokenOperator.ThrowOnCombinedOperator();
    }

    sealed class Before : IFilteredTokenOperator
    {
        public void Activate( Action<IFilteredTokenOperator> collector ) => collector( this );

        public void Apply( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
        {
            // We need to retrieve the tokens from the PREVIOUS inner, the source inner of the mono-location LocationMatcher
            // that is our previous.
            Throw.DebugAssert( "We are working on the result of a mono-location LocationCardinality.",
                               context.Previous.FilteredTokens == input
                               && (context.Previous.Operator == LocationCardinality.SingleCardinality
                                   || (context.Previous.Operator is LocationCardinality card
                                       && card.Kind is LocationCardinality.LocationKind.Single
                                                       or LocationCardinality.LocationKind.First
                                                       or LocationCardinality.LocationKind.Last)) );
            var previousInput = context.Previous.Previous;
            Throw.DebugAssert( previousInput != null );
            do
            {
                Throw.DebugAssert( "A LocationCardinality is followed by a matcher and the root is a SyntaxBorder.", !previousInput.IsRoot );
                previousInput = previousInput.Previous;
            }
            while( !previousInput.IsSyntaxBorder );

            var builder = context.SharedBuilder;
            var eInput = new FilteredTokenSpanEnumerator( input, context.UnfilteredTokens );
            var ePrevious = new FilteredTokenSpanEnumerator( previousInput.FilteredTokens, context.UnfilteredTokens );
            while( eInput.NextEach() )
            {
                eInput.NextMatch(); 
                Throw.DebugAssert( "Working on the result of a mono-location LocationCardinality.", !eInput.NextMatch() );

                // Previous corresponding 'each' necessarily exists.
                ePrevious.NextEach();
                Throw.DebugAssert( eInput.CurrentEachIndex == ePrevious.CurrentEachIndex );
                ePrevious.NextMatch(); 

                builder.StartNewEach();
                int beg = ePrevious.CurrentMatch.Span.Beg;
                int end = eInput.CurrentMatch.Span.Beg;
                Throw.DebugAssert( "The input mono match is inside one of the previous matches.", beg <= end );
                if( beg == end )
                {
                    context.SetFailedResult( $"No token exist 'before'.", eInput );
                    return;
                }
                builder.AddMatch( new TokenSpan( beg, end ) );
            }
            context.SetResult( builder );
        }

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "before" );

        public override string ToString() => "before";

    }

    sealed class After : IFilteredTokenOperator
    {
        public void Activate( Action<IFilteredTokenOperator> collector ) => collector( this );

        public void Apply( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
        {
            // We need to retrieve the tokens from the PREVIOUS inner, the source inner of the mono-location LocationMatcher
            // or the Before (when we are in a between) that is our previous.
            Throw.DebugAssert( "We are working on the result of a mono-location LocationCardinality or a Before.",
                               context.Previous.FilteredTokens == input
                               && (context.Previous.Operator is Before
                                   || context.Previous.Operator == LocationCardinality.SingleCardinality
                                   || (context.Previous.Operator is LocationCardinality card
                                       && card.Kind is LocationCardinality.LocationKind.Single
                                                    or LocationCardinality.LocationKind.First
                                                    or LocationCardinality.LocationKind.Last)) );
            // If we are after a Before, the Before is our previous input (not the input of the 'between' that activated
            // the Before and this After).
            var previousInput = context.Previous.Previous;
            Throw.DebugAssert( previousInput != null );
            if( context.Previous.Operator is not Before )
            {
                do
                {
                    Throw.DebugAssert( "A LocationCardinality is followed by a matcher and the root is a SyntaxBorder.",
                                       !previousInput.IsRoot );
                    previousInput = previousInput.Previous;
                }
                while( !previousInput.IsSyntaxBorder );
            }

            var builder = context.SharedBuilder;
            var eInput = new FilteredTokenSpanEnumerator( input, context.UnfilteredTokens );
            var ePrevious = new FilteredTokenSpanEnumerator( previousInput.FilteredTokens, context.UnfilteredTokens );
            while( eInput.NextEach() )
            {
                eInput.NextMatch();
                Throw.DebugAssert( "Working on the result of a mono-location LocationCardinality.", !eInput.NextMatch() );

                // Previous corresponding 'each' necessarily exists.
                ePrevious.NextEach();
                Throw.DebugAssert( eInput.CurrentEachIndex == ePrevious.CurrentEachIndex );

                int beg = eInput.CurrentMatch.Span.End;
                // Find the end of the last match of the previous.
                ePrevious.NextMatch();
                int end = ePrevious.CurrentMatch.Span.End;
                while( ePrevious.NextMatch() )
                {
                    end = ePrevious.CurrentMatch.Span.End;
                }
                builder.StartNewEach();
                Throw.DebugAssert( "The input mono match is inside one of the previous matches.", beg <= end );
                if( beg == end )
                {
                    context.SetFailedResult( $"No token exist 'after'.", eInput );
                    return;
                }
                builder.AddMatch( new TokenSpan( beg, end ) );
            }
            context.SetResult( builder );
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
