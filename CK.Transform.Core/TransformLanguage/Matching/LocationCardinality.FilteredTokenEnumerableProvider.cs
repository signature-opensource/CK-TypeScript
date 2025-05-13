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
            case LocationKind.Last:
                HandleLast( context, input );
                break;
            case LocationKind.All:
                HandleAll( context, input );
                break;
            default:
                HandleEach( context, input );
                break;
        }
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
            if( builder.CurrentEachNumber == 0 )
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
            context.SetFailedResult( $"Expected a last match but got none.", null );
            return;
        }
        var builder = context.SharedBuilder;
        var e = new FilteredTokenSpanEnumerator( input, context.UnfilteredTokens );
        while( e.NextEach() )
        {
            builder.StartNewEach();
            int matchCount = 0;
            while( e.NextMatch() ) ++matchCount;
            if( _expectedMatchCount > 0 && _expectedMatchCount != matchCount )
            {
                context.SetFailedResult( $"Expected {_expectedMatchCount} matches but got {matchCount}.", e );
                return;
            }
            if( _offset < matchCount )
            {
                context.SetFailedResult( $"Expected '{ToString()}' but got {matchCount} matches.", e );
                return;
            }
            builder.AddMatch( input[e.CurrentInputIndex - _offset].Span );
        }
    }


    void HandleAll( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
    {
        // We must check this edge-case: the source code is empty. 
        if( input.Count == 0 )
        {
            context.SetFailedResult( $"Expected some match but got none.", null );
            return;
        }
        // "all" without expected match count is a no-op.
        if( _expectedMatchCount > 0 )
        {
            var e = new FilteredTokenSpanEnumerator( input, context.UnfilteredTokens );
            while( e.NextEach() )
            {
                int matchCount = 0;
                while( e.NextMatch() ) ++matchCount;
                if( _expectedMatchCount != matchCount )
                {
                    context.SetFailedResult( $"Expected {_expectedMatchCount} matches but got {matchCount}.", e );
                    return;
                }
            }
        }
        context.SetUnchangedResult();
    }

    void HandleEach( IFilteredTokenOperatorContext context, IReadOnlyList<FilteredTokenSpan> input )
    {
        Throw.DebugAssert( _kind == LocationKind.Each );
        // We must check this edge-case: the source code is empty. 
        if( input.Count == 0 )
        {
            context.SetFailedResult( $"Expected some match but got none.", null );
            return;
        }
        if( _expectedMatchCount != 0 && _expectedMatchCount != input.Count )
        {
            context.SetFailedResult( $"Expected {_expectedMatchCount} matches but got {input.Count}.", null );
            return;
        }
        if( input[^1].EachIndex == input.Count - 1 )
        {
            context.SetUnchangedResult();
        }
        else
        {
            context.SetResult( input.Select( ( m, index ) => new FilteredTokenSpan( index, 0, m.Span ) ).ToArray() );
        }
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
