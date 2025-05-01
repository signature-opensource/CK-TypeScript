using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace CK.Transform.Core;

/// <summary>
/// Captures "<see cref="LocationCardinality"/> <see cref="Matcher"/>".
/// </summary>
public sealed class LocationMatcher : SourceSpan, ITokenFilter
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

    internal static LocationMatcher? Parse( ref TokenizerHead head, bool monoLocationOnly = false )
    {
        int begSpan = head.LastTokenIndex + 1;
        var cardinality = LocationCardinality.Match( ref head, monoLocationOnly );
        var matcher = SpanMatcher.Match( ref head );
        return matcher == null
                ? null
                : head.AddSpan( new LocationMatcher( begSpan, head.LastTokenIndex + 1 ) );
    }

    IEnumerable<IEnumerable<IEnumerable<SourceToken>>> ITokenFilter.GetScopedTokens( ScopedTokensBuilder builder )
    {
        Throw.DebugAssert( CheckValid() );
        var matcher = Matcher;
        var inner = matcher.GetScopedTokens( builder );
        if( builder.HasError ) return ScopedTokensBuilder.EmptyResult;

        var cardinality = Cardinality;
        var kind = cardinality?.Kind ?? LocationCardinality.LocationKind.Single;
        return kind switch
        {
            LocationCardinality.LocationKind.Single => HandleSingle( builder.Monitor, matcher, inner ),
            LocationCardinality.LocationKind.First => HandleFirst( builder.Monitor, matcher, inner, cardinality! ),
            LocationCardinality.LocationKind.Last => HandleLast( builder.Monitor, matcher, inner, cardinality! ),
            LocationCardinality.LocationKind.All => HandleAll( builder.Monitor, matcher, inner, cardinality! ),
            _ => HandleEach( builder.Monitor, matcher, inner, cardinality! )
        };

        static IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleSingle( IActivityMonitor monitor,
                                                                                SpanMatcher matcher,
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
                    monitor.Error( $"""
                                    Expected single '{matcher}' but got {each.Count()} in:
                                    {DumpRanges( each )}
                                    """ );
                    break;
                }
            }
            monitor.Error( $"""
                            Expected single '{matcher}' but got none.
                            """ );
        }

        static IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleFirst( IActivityMonitor monitor,
                                                                               SpanMatcher matcher,
                                                                               IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner,
                                                                               LocationCardinality cardinality )
        {
            foreach( var each in inner )
            {
                int count = HandleExpectedMatchCount( monitor, matcher, cardinality, each );
                if( count == -2 ) break;

                var first = each.Skip( cardinality.Offset - 1 ).FirstOrDefault();
                if( first != null )
                {
                    yield return [first];
                }
                else
                {
                    monitor.Error( $"""
                                    Expected '{cardinality} {matcher}' but got {count} matches in:
                                    {DumpRanges( each )}
                                    """ );
                    break;
                }
            }
        }

        static IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleLast( IActivityMonitor monitor,
                                                                              SpanMatcher matcher,
                                                                              IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner,
                                                                              LocationCardinality cardinality )
        {
            foreach( var each in inner )
            {
                int count = HandleExpectedMatchCount( monitor, matcher, cardinality, each );
                if( count == -2 ) break;
                if( count == -1 ) count = each.Count();
                int at = count - cardinality.Offset + 1;
                if( at >= 0 && at < count )
                {
                    yield return [each.ElementAt( at )];
                }
                else
                {
                    monitor.Error( $"""
                                    Expected '{cardinality} {matcher}' but got {count} matches in:
                                    {DumpRanges( each )}
                                    """ );
                    break;
                }
            }
        }

        static IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleAll( IActivityMonitor monitor,
                                                                             SpanMatcher matcher,
                                                                             IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner,
                                                                             LocationCardinality cardinality )
        {
            if( cardinality.ExpectedMatchCount != 0 )
            {
                foreach( var each in inner )
                {
                    int count = HandleExpectedMatchCount( monitor, matcher, cardinality, each );
                    if( count == -2 ) return ScopedTokensBuilder.EmptyResult;
                }
            }
            return inner;
        }

        static IEnumerable<IEnumerable<IEnumerable<SourceToken>>> HandleEach( IActivityMonitor monitor,
                                                                              SpanMatcher matcher,
                                                                              IEnumerable<IEnumerable<IEnumerable<SourceToken>>> inner,
                                                                              LocationCardinality cardinality )
        {
            if( cardinality.ExpectedMatchCount != 0 )
            {
                foreach( var each in inner )
                {
                    int count = HandleExpectedMatchCount( monitor, matcher, cardinality, each );
                    if( count == -2 ) break;
                    foreach( var r in each ) yield return [r];
                }
            }
        }

        static int HandleExpectedMatchCount( IActivityMonitor monitor,
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
                    monitor.Error( $"""
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
}
