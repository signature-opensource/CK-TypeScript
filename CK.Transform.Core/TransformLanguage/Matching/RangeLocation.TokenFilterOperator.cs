using System;
using CK.Core;
using System.Text;

namespace CK.Transform.Core;

public sealed partial class RangeLocation : ITokenFilterOperator
{
    /// <inheritdoc />
    public void Activate( Action<ITokenFilterOperator> collector )
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

    void ITokenFilterOperator.Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        ITokenFilterOperator.ThrowOnCombinedOperator();
    }

    sealed class Before : ITokenFilterOperator
    {
        public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

        public void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
        {
            // We need to retrieve the tokens from the PREVIOUS inner, the source inner of the mono-location LocationMatcher
            // that is our previous.
            Throw.DebugAssert( "We are working on the result of a mono-location LocationCardinality.",
                               input.Operator == LocationCardinality.SingleCardinality
                               || (input.Operator is LocationCardinality card
                                        && card.Kind is LocationCardinality.LocationKind.Single
                                                     or LocationCardinality.LocationKind.First
                                                     or LocationCardinality.LocationKind.Last) );
            var previousInput = input.Previous;
            Throw.DebugAssert( previousInput != null );
            do
            {
                Throw.DebugAssert( "A LocationCardinality is followed by a matcher and the root is a SyntaxBorder.", !previousInput.IsRoot );
                previousInput = previousInput.Previous;
            }
            while( !previousInput.IsSyntaxBorder );

            var builder = context.SharedBuilder;
            var eInput = input.CreateTokenEnumerator();
            var ePrevious = previousInput.CreateTokenEnumerator();
            while( eInput.NextEach( skipEmpty: true ) )
            {
                eInput.NextMatch();
                // Our end is the start of the mono-location matcher.
                int end = eInput.CurrentMatch.Span.Beg;
                Throw.DebugAssert( "Working on the result of a mono-location LocationCardinality.", !eInput.NextMatch() );

                // Previous corresponding 'each' necessarily exists.
                ePrevious.NextEach( skipEmpty: true );
                ePrevious.NextMatch();
                // Our start is the start of the first match in previous.
                int beg = ePrevious.CurrentMatch.Span.Beg;

                Throw.DebugAssert( "The input mono match is inside one of the previous matches.", beg <= end );
                if( beg < end )
                {
                    builder.AddMatch( new TokenSpan( beg, end ) );
                }
                builder.StartNewEach( skipEmpty: true );
            }
            context.SetResult( builder );
        }

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "before" );

        public override string ToString() => "before";

    }

    sealed class After : ITokenFilterOperator
    {
        public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

        public void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
        {
            // We need to retrieve the tokens from the PREVIOUS inner, the source inner of the mono-location LocationMatcher
            // or the Before (when we are in a between) that is our previous.
            Throw.DebugAssert( "We are working on the result of a mono-location LocationCardinality or a Before.",
                               input.Operator is Before
                               || input.Operator == LocationCardinality.SingleCardinality
                               || (input.Operator is LocationCardinality card
                                       && card.Kind is LocationCardinality.LocationKind.Single
                                                    or LocationCardinality.LocationKind.First
                                                    or LocationCardinality.LocationKind.Last) );
            // If we are after a Before, the Before is our previous input (not the input of the 'between' that activated
            // the Before and this After).
            var previousInput = input.Previous;
            Throw.DebugAssert( previousInput != null );
            if( input.Operator is not Before )
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
            var eInput = input.CreateTokenEnumerator();
            var ePrevious = previousInput.CreateTokenEnumerator();
            while( eInput.NextEach( skipEmpty: true ) )
            {
                eInput.NextMatch();
                // Our start is the end of the mono-location matcher.
                int beg = eInput.CurrentMatch.Span.End;

                Throw.DebugAssert( "Working on the result of a mono-location LocationCardinality.", !eInput.NextMatch() );

                // Previous corresponding 'each' necessarily exists.
                ePrevious.NextEach( skipEmpty: true );

                // Find the end of the last match of the previous.
                ePrevious.NextMatch();
                int end = ePrevious.CurrentMatch.Span.End;
                while( ePrevious.NextMatch() )
                {
                    end = ePrevious.CurrentMatch.Span.End;
                }
                Throw.DebugAssert( "The input mono match is inside one of the previous matches.", beg <= end );
                if( beg < end )
                {
                    builder.AddMatch( new TokenSpan( beg, end ) );
                }
                builder.StartNewEach( skipEmpty: true );
            }
            context.SetResult( builder );
        }

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "after" );

        public override string ToString() => "after";
    }

    /// <inheritdoc />
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
