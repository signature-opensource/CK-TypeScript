using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace CK.Transform.Core;

public sealed partial class LocationCardinality : IFilteredTokenEnumerableProvider
{
    sealed class Single : IFilteredTokenEnumerableProvider
    {
        public void Activate( Action<IFilteredTokenEnumerableProvider> collector ) => collector( this );

        Func<ITokenFilterBuilderContext,
             IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
             IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> IFilteredTokenEnumerableProvider.GetFilteredTokenProjection()
        {
            return HandleSingle;
        }

        public StringBuilder Describe( StringBuilder b, bool parsable ) => b.Append( "single" );

        public override string ToString() => "single";

    }

    /// <summary>
    /// Singleton "single" provider that is the default cardinality.
    /// </summary>
    public static readonly IFilteredTokenEnumerableProvider SingleCardinality = new Single();

    public void Activate( Action<IFilteredTokenEnumerableProvider> collector ) => collector( this );

    Func<ITokenFilterBuilderContext,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>,
         IEnumerable<IEnumerable<IEnumerable<SourceToken>>>> IFilteredTokenEnumerableProvider.GetFilteredTokenProjection()
    {
        Throw.DebugAssert( CheckValid() );
        return _kind switch
        {
            LocationKind.Single => HandleSingle,
            LocationKind.First => HandleFirst,
            LocationKind.Last => HandleLast,
            // All without ExpectedMatchCount is a no-op constraint.
            LocationKind.All => _expectedMatchCount == 0
                                    ? HandleAll
                                    : HandleAllWithExpectedCount,
            _ => HandleEach
        };
    }

    static IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleSingle( ITokenFilterBuilderContext c,
                                                                            IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        foreach( var each in inner )
        {
            var single = each.SingleOrDefault();
            if( single != null )
            {
                yield return [single];
                yield break;
            }
            else
            {
                c.Fail( $"Expected single match but got {each.Count()}" );
                yield break;
            }
        }
        c.Fail( $"Expected single match but got none: Tokens are empty." );
    }


    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleFirst( ITokenFilterBuilderContext c,
                                                                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( CheckValid() && _kind == LocationKind.First );
        foreach( var each in inner )
        {
            int count = HandleExpectedMatchCount( c, this, each );
            if( count == -2 ) break;

            var first = each.Skip( _offset - 1 ).FirstOrDefault();
            if( first != null )
            {
                yield return [first];
            }
            else
            {
                c.Fail( $"Expected '{ToString()}' but got {count} matches" );
                break;
            }
        }
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleLast( ITokenFilterBuilderContext c,
                                                                   IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( _kind == LocationKind.Last );
        foreach( var each in inner )
        {
            int count = HandleExpectedMatchCount( c, this, each );
            if( count == -2 ) break;
            if( count == -1 ) count = each.Count();
            int at = count - _offset;
            if( at >= 0 && at < count )
            {
                yield return [each.ElementAt( at )];
            }
            else
            {
                c.Fail( $"Expected '{ToString()}' but got {count} matches" );
                break;
            }
        }
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleAllWithExpectedCount( ITokenFilterBuilderContext c,
                                                                                   IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( _kind == LocationKind.All && _expectedMatchCount > 0 );
        foreach( var each in inner )
        {
            HandleExpectedMatchCount( c, this, each );
        }
        return inner;
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleAll( ITokenFilterBuilderContext c,
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

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleEach( ITokenFilterBuilderContext c,
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

    static int HandleExpectedMatchCount( ITokenFilterBuilderContext c,
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
