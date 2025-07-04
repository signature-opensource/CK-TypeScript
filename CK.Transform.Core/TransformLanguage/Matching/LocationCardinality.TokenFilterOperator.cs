using CK.Core;
using System;
using System.Linq;
using System.Text;

namespace CK.Transform.Core;

public sealed partial class LocationCardinality : ITokenFilterOperator
{
    sealed class Single : ITokenFilterOperator
    {
        public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

        public void Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input ) => HandleSingle( context, input );

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "single" );

        public override string ToString() => "single";

    }

    /// <summary>
    /// Singleton "single" provider that is the default cardinality.
    /// </summary>
    public static readonly ITokenFilterOperator SingleCardinality = new Single();

    public void Activate( Action<ITokenFilterOperator> collector ) => collector( this );

    void ITokenFilterOperator.Apply( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
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

    static void HandleSingle( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        var e = input.CreateTokenEnumerator();
        while( e.NextEach( skipEmpty: false ) )
        {
            int count = 0;
            while( e.NextMatch() ) ++count;
            if( count != 1 && (!context.AllowEmpty || count != 0) )
            {
                context.SetFailedResult( $"Expected a single match but got {count}.", e );
            }
        }
        context.SetUnchangedResult();
    }

    void HandleFirst( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        Throw.DebugAssert( CheckValid() && _kind == LocationKind.First );
        var builder = context.SharedBuilder;
        var e = input.CreateTokenEnumerator();
        while( e.NextEach( skipEmpty: false ) )
        {
            int matchCount = 0;
            while( e.NextMatch() )
            {
                if( ++matchCount == _offset )
                {
                    builder.AddMatch( e.CurrentMatch.Span );
                    break;
                }
            }
            if( builder.CurrentMatchNumber == 0 && (!context.AllowEmpty || matchCount != 0) )
            {
                context.SetFailedResult( $"Expected '{ToString()}' but got {matchCount} matches.", e );
                return;
            }
            builder.StartNewEach( skipEmpty: false );
            if( _expectedMatchCount > 0 )
            {
                while( e.NextMatch() ) ++matchCount;
                if( _expectedMatchCount != matchCount && (!context.AllowEmpty || matchCount != 0) )
                {
                    context.SetFailedResult( $"Expected {_expectedMatchCount} matches but got {matchCount}.", e );
                    return;
                }
            }
        }
        context.SetResult( builder );
    }

    void HandleLast( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        Throw.DebugAssert( _kind == LocationKind.Last );
        var builder = context.SharedBuilder;
        var e = input.CreateTokenEnumerator();
        while( e.NextEach( skipEmpty: false ) )
        {
            int matchCount = 0;
            while( e.NextMatch() ) ++matchCount;
            if( matchCount > 0 || !context.AllowEmpty )
            {
                if( _expectedMatchCount > 0 && _expectedMatchCount != matchCount )
                {
                    context.SetFailedResult( $"Expected {_expectedMatchCount} matches but got {matchCount}.", e );
                    return;
                }
                if( _offset > matchCount )
                {
                    context.SetFailedResult( $"Expected '{ToString()}' but got {matchCount} matches.", e );
                    return;
                }
                builder.AddMatch( e.Input[e.CurrentInputIndex - _offset].Span );
            }
            builder.StartNewEach( skipEmpty: false );
        }
        context.SetResult( builder );
    }


    void HandleAll( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        // "all" without expected match count is a no-op.
        if( _expectedMatchCount > 0 )
        {
            var e = input.CreateTokenEnumerator();
            while( e.NextEach( skipEmpty: false ) )
            {
                int matchCount = 0;
                while( e.NextMatch() ) ++matchCount;
                if( _expectedMatchCount != matchCount && (!context.AllowEmpty || matchCount != 0) )
                {
                    context.SetFailedResult( $"Expected {_expectedMatchCount} matches but got {matchCount}.", e );
                    return;
                }
            }
        }
        context.SetUnchangedResult();
    }

    void HandleEach( ITokenFilterOperatorContext context, ITokenFilterOperatorSource input )
    {
        Throw.DebugAssert( _kind == LocationKind.Each && input.Tokens.IsValid );
        // Each is a very special operator: it is implemented internally.
        var internalInputMatches = input.Tokens.ArrayMatches;
        int inputMatchCount = internalInputMatches.Length;
        if( _expectedMatchCount != 0 && _expectedMatchCount != inputMatchCount && (!context.AllowEmpty || inputMatchCount != 0) )
        {
            context.SetFailedResult( $"Expected {_expectedMatchCount} matches but got {inputMatchCount}.", null );
            return;
        }
        // When all matches already are "each" buckets, we have nothing to do.
        if( internalInputMatches[^1].EachIndex == inputMatchCount - 1 )
        {
            context.SetUnchangedResult();
        }
        else
        {
            // Promotes all matches to the "each" level.
            context.SetResult( internalInputMatches.Select( ( m, index ) => new TokenMatch( index, 0, m.Span ) ).ToArray() );
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
