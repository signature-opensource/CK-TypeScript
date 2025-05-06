using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace CK.Transform.Core;

using TokenFilterFunc = Func<TokenFilterBuilderContext, IEnumerable<IEnumerable<IEnumerable<SourceToken>>>, IEnumerable<IEnumerable<IEnumerable<SourceToken>>>>;

/// <summary>
/// Captures "<see cref="LocationCardinality"/> <see cref="Matcher"/>".
/// </summary>
public sealed class LocationMatcher : SourceSpan, IFilteredTokenEnumerableProvider
{
    LocationMatcher( int beg, int end )
        : base( beg, end )
    {
    }

    /// <summary>
    /// Checks that <see cref="Matcher"/> is valid.
    /// </summary>
    /// <returns>True if this span is valid.</returns>
    [MemberNotNullWhen( true, nameof( Matcher ) )]
    public override bool CheckValid()
    {
        return base.CheckValid() && Matcher != null;
    }

    /// <summary>
    /// Gets the optional match cardinality.
    /// When null, the match must be considered "single".
    /// </summary>
    public LocationCardinality? Cardinality => Children.FirstChild as LocationCardinality;

    /// <summary>
    /// Gets the span matcher.
    /// Never null when <see cref="CheckValid()"/> is true.
    /// </summary>
    public SpanMatcher? Matcher
    {
        get
        {
            var c = Children.FirstChild;
            return c as SpanMatcher ?? c?.NextSibling as SpanMatcher;
        }
    }

    internal static LocationMatcher? Parse( TransformerHost.Language language, ref TokenizerHead head, bool monoLocationOnly = false )
    {
        int begSpan = head.LastTokenIndex + 1;
        var cardinality = LocationCardinality.Match( ref head, monoLocationOnly );
        var matcher = SpanMatcher.Match( language, ref head );
        return matcher == null
                ? null
                : head.AddSpan( new LocationMatcher( begSpan, head.LastTokenIndex + 1 ) );
    }

    TokenFilterFunc IFilteredTokenEnumerableProvider.GetFilteredTokenProjection()
    {
        Throw.DebugAssert( CheckValid() );
        var matcherFunc = Matcher.GetFilteredTokenProjection();

        var cardinality = Cardinality;
        var kind = cardinality?.Kind ?? LocationCardinality.LocationKind.Single;
        return kind switch
        {
            LocationCardinality.LocationKind.Single => ( c, inner ) => HandleSingle( c, matcherFunc( c, inner ) ),
            LocationCardinality.LocationKind.First => ( c, inner ) => HandleFirst( c, matcherFunc( c, inner ) ),
            LocationCardinality.LocationKind.Last => ( c, inner ) => HandleLast( c, matcherFunc( c, inner)),
            LocationCardinality.LocationKind.All => cardinality!.ExpectedMatchCount == 0 
                                                        ? matcherFunc // All without ExpectedMatchCount is a no-op constraint.
                                                        : ( c, inner ) => HandleAllCount( c, matcherFunc( c, inner ) ),
            _ => ( c, inner ) => HandleEach( c, matcherFunc( c, inner ) )
        };
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleSingle( TokenFilterBuilderContext c,
                                                                     IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( CheckValid() );
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
                c.Error( $"""
                         Expected single '{Matcher}' but got {each.Count()} in:
                         {DumpRanges( each )}
                         """ );
                break;
            }
        }
        c.Error( $"""
                 Expected single '{Matcher}' but got none.
                 """ );
    }


    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleFirst( TokenFilterBuilderContext c,
                                                                    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( CheckValid() && Cardinality != null && Cardinality.Kind == LocationCardinality.LocationKind.First );
        foreach( var each in inner )
        {
            int count = HandleExpectedMatchCount( c, Matcher, Cardinality, each );
            if( count == -2 ) break;

            var first = each.Skip( Cardinality.Offset - 1 ).FirstOrDefault();
            if( first != null )
            {
                yield return [first];
            }
            else
            {
                c.Error( $"""
                          Expected '{Cardinality} {Matcher}' but got {count} matches in:
                          {DumpRanges( each )}
                          """ );
                break;
            }
        }
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleLast( TokenFilterBuilderContext c,
                                                                   IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( CheckValid() && Cardinality != null && Cardinality.Kind == LocationCardinality.LocationKind.Last );
        foreach( var each in inner )
        {
            int count = HandleExpectedMatchCount( c, Matcher, Cardinality, each );
            if( count == -2 ) break;
            if( count == -1 ) count = each.Count();
            int at = count - Cardinality.Offset + 1;
            if( at >= 0 && at < count )
            {
                yield return [each.ElementAt( at )];
            }
            else
            {
                c.Error( $"""
                         Expected '{Cardinality} {Matcher}' but got {count} matches in:
                         {DumpRanges( each )}
                         """ );
                break;
            }
        }
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleAllCount( TokenFilterBuilderContext c,
                                                                       IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( CheckValid()
                           && Cardinality != null
                           && Cardinality.Kind == LocationCardinality.LocationKind.All
                           && Cardinality.ExpectedMatchCount > 0 );
        foreach( var each in inner )
        {
            int count = HandleExpectedMatchCount( c, Matcher, Cardinality, each );
            if( count == -2 ) return IFilteredTokenEnumerableProvider.EmptyFilteredTokens;
        }
        return inner;
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleEach( TokenFilterBuilderContext c,
                                                                   IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner )
    {
        Throw.DebugAssert( CheckValid() && Cardinality != null && Cardinality.Kind == LocationCardinality.LocationKind.Each );
        if( Cardinality.ExpectedMatchCount != 0 )
        {
            foreach( var each in inner )
            {
                int count = HandleExpectedMatchCount( c, Matcher, Cardinality, each );
                if( count == -2 ) break;
                foreach( var r in each ) yield return [r];
            }
        }
    }

    static int HandleExpectedMatchCount( TokenFilterBuilderContext c,
                                         SpanMatcher matcher,
                                         LocationCardinality cardinality,
                                         IEnumerable<IEnumerable<SourceToken>> each )
    {
        int count = -1;
        if( cardinality.ExpectedMatchCount != 0 )
        {
            count = each.Count();
            if( count != cardinality.ExpectedMatchCount )
            {
                c.Error( $"""
                         Expected {cardinality} '{matcher}' but got {count} matches in:
                         {DumpRanges( each )}
                         """ );
                count = -2;
            }
        }
        return count;
    }

    static string DumpRanges( IEnumerable<IEnumerable<SourceToken>> each )
    {
        var b = new StringBuilder();
        int iRange = 0;
        foreach( var r in each )
        {
            b.Append( "--- (range n°" ).Append( ++iRange ).AppendLine( ") ---" );
            r.Select( t => t.Token ).Write( b ).AppendLine();
            b.AppendLine( "---" );
        }
        return b.ToString();
    }
}
