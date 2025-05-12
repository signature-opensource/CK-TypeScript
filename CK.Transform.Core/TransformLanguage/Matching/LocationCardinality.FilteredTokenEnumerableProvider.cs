using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace CK.Transform.Core;

public sealed partial class LocationCardinality : IFilteredTokenOperator
{
    sealed class Single : IFilteredTokenOperator
    {
        public void Activate( Action<IFilteredTokenOperator> collector ) => collector( this );

        public void Apply( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
        {
            // We must check this edge-case: the source code is empty. 
            if( input.Count == 0 )
            {
                context.SetFailedResult( $"Expected a single match but got none.", null );
                return;
            }
            var e = new FilteredTokenSpanEnumerator( input, context.UnfilteredTokens );
            while( e.NextEach() )
            {
                int count = 0;
                while( e.NextMatch() ) ++count;
                if( count != 1 )
                {
                    context.SetFailedResult( $"Expected a single match but got {count}.", e );
                }
            }
            context.SetUnchangedResult();
        }

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "single" );

        public override string ToString() => "single";

    }

    /// <summary>
    /// Singleton "single" provider that is the default cardinality.
    /// </summary>
    public static readonly IFilteredTokenOperator SingleCardinality = new Single();

    public void Activate( Action<IFilteredTokenOperator> collector ) => collector( this );

    void IFilteredTokenOperator.Apply( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
    {
        Throw.DebugAssert( CheckValid() );
        switch( _kind )
        {
            case LocationKind.Single:
                HandleSingle( context, input );
                break;
            case LocationKind.First:
                HandleFirst( context, input );
                break;
            LocationKind.Last:
                HandleLast( context, input );
                break;
            // All without ExpectedMatchCount is a no-op constraint.
            LocationKind.All => _expectedMatchCount == 0
                                    ? HandleAll
                                    : HandleAllWithExpectedCount,
            _ => HandleEach
        };
    }


    static void HandleSingle( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
    {
        // We must check this edge-case: the source code is empty. 
        if( input.Count == 0 )
        {
            context.SetFailedResult( $"Expected a single match but got none.", null );
            return;
        }
        var e = new FilteredTokenSpanEnumerator( input, context.UnfilteredTokens );
        while( e.NextEach() )
        {
            int count = 0;
            while( e.NextMatch() ) ++count;
            if( count != 1 )
            {
                context.SetFailedResult( $"Expected a single match but got {count}.", e );
            }
        }
        context.SetUnchangedResult();
    }


    void HandleFirst( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
    {
        Throw.DebugAssert( CheckValid() && _kind == LocationKind.First );
        // We must check this edge-case: the source code is empty. 
        if( input.Count == 0 )
        {
            context.SetFailedResult( $"Expected a first match but got none.", null );
            return;
        }
        var builder = context.SharedBuilder;
        var e = new FilteredTokenSpanEnumerator( input, context.UnfilteredTokens );
        while( e.NextEach() )
        {
            builder.StartNewEach();
            int matchCount = 0;
            while( e.NextMatch() )
            {
                if( ++matchCount == _offset )
                {
                    builder.AddMatch( e.CurrentMatch.Span );
                    break;
                }
            }
            if( builder.CurrentEachCount == 0 )
            {
                context.SetFailedResult( $"Expected '{ToString()}' but got {matchCount} matches.", e );
                return;
            }
            if( _expectedMatchCount > 0 )
            {
                while( e.NextMatch() ) ++matchCount;
                if( _expectedMatchCount != matchCount )
                {
                    context.SetFailedResult( $"Expected {_expectedMatchCount} matches but got {matchCount}.", e );
                    return;
                }
            }
        }
        context.SetResult( builder );
    }

    void HandleLast( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
    {
        Throw.DebugAssert( _kind == LocationKind.Last );
        // We must check this edge-case: the source code is empty. 
        if( input.Count == 0 )
        {
            context.SetFailedResult( $"Expected a first match but got none.", null );
            return;
        }
        var builder = context.SharedBuilder;
        var e = new FilteredTokenSpanEnumerator( input, context.UnfilteredTokens );
        int inputIndex = 0;
        while( e.NextEach() )
        {
            builder.StartNewEach();
            int matchCount = 0;
            while( e.NextMatch() ) ++matchCount;
            inputIndex += matchCount;
            if( )
        }
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleAllWithExpectedCount( IFilteredTokenOperatorContext c,
                                                                                   IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( _kind == LocationKind.All && _expectedMatchCount > 0 );
        foreach( var each in inner )
        {
            HandleExpectedMatchCount( c, this, each );
        }
        return inner;
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleAll( IFilteredTokenOperatorContext c,
                                                                  IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( _kind == LocationKind.All && _expectedMatchCount == 0 );
        foreach( var each in inner )
        {
            if( !each.Any() )
            {
                c.Fail( $"'all' expects at least one match." );
            }
        }
        return inner;
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleEach( IFilteredTokenOperatorContext c,
                                                                   IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( _kind == LocationKind.Each );
        if( _expectedMatchCount != 0 )
        {
            foreach( var each in inner )
            {
                int count = HandleExpectedMatchCount( c, this, each );
                if( count == -2 ) break;
                foreach( var r in each ) yield return [r];
            }
        }
        else
        {
            foreach( var each in inner )
            {
                foreach( var r in each ) yield return [r];
            }
        }
    }

    static int HandleExpectedMatchCount( IFilteredTokenOperatorContext c,
                                         LocationCardinality cardinality,
                                         IEnumerable<IEnumerable<SourceToken>> each )
    {
        int count = -1;
        if( cardinality.ExpectedMatchCount != 0 )
        {
            count = each.Count();
            if( count != cardinality.ExpectedMatchCount )
            {
                c.Fail( $"Expected '{cardinality}' but got {count} matches." );
                count = -2;
            }
        }
        return count;
    }

    public StringBuilder Describe( StringBuilder b, bool parsable )
    {
        if( !parsable ) b.Append( "[Cardinality] " );
        return Write( b );
    }

    StringBuilder Write( StringBuilder b )
    {
        switch( _kind )
        {
            case LocationKind.Each:
                return _expectedMatchCount == 0
                                                ? b.Append( "each" )
                                                : b.Append( "each " ).Append( _expectedMatchCount );
            case LocationKind.All:
                return _expectedMatchCount == 0
                                                ? b.Append( "all" )
                                                : b.Append( "all " ).Append( _expectedMatchCount );
            case LocationKind.Single: return b.Append( "single" );
            case LocationKind.First:
                b.Append( "first" );
                goto default;
            case LocationKind.Last:
                b.Append( "last" );
                goto default;
            default:
                if( _offset > 1 )
                {
                    b.Append( ' ' ).Append( _offset );
                }
                return _expectedMatchCount == 0
                            ? b
                            : b.Append( " out of " ).Append( _expectedMatchCount );
        }
    }

    /// <summary>
    /// Gets the formatted string (as it can be parsed).
    /// </summary>
    /// <returns>The readable (and parsable) string.</returns>
    public override string ToString() => Write( new StringBuilder() ).ToString();

}
