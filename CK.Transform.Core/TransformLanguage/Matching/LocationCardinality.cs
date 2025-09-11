using CK.Core;

namespace CK.Transform.Core;

/// <summary>
/// Models location cardinality constraints:
/// <list type="bullet">
///     <item>single</item>
///     <item>first [+n] [out of n]</item>
///     <item>last [-n] [out of n]</item>
///     <item>all [n]</item>
///     <item>each [n]</item>
/// </list>
/// This is a <see cref="ITokenFilterOperator"/>. The <see cref="SingleCardinality"/> singleton
/// is the default cardinality provider.
/// </summary>
public sealed partial class LocationCardinality : SourceSpan
{
    int _expectedMatchCount;
    int _offset;
    LocationKind _kind;

    /// <summary>
    /// The possible <see cref="LocationKind"/>.
    /// </summary>
    public enum LocationKind
    {
        /// <summary>
        /// Single location.
        /// </summary>
        Single,

        /// <summary>
        /// First or nth location ("first 2" is the second location, "first 1" is the same as "first").
        /// <see cref="ExpectedMatchCount"/> applies when not 0: "first 2 out of 3".
        /// </summary>
        First,

        /// <summary>
        /// First location or the nth location from the last one ("last 2" is the penultimate, "last 1" is the same as "last").
        /// <see cref="ExpectedMatchCount"/> applies when not 0: "last 2 out of 3".
        /// </summary>
        Last,

        /// <summary>
        /// All possible locations: the different matches are considered as a unique
        /// range (with holes in it).
        /// <see cref="ExpectedMatchCount"/> applies when not 0: "all 3".
        /// </summary>
        All,

        /// <summary>
        /// Each possible locations: the different matches are independent
        /// from each other.
        /// <see cref="ExpectedMatchCount"/> applies when not 0: "each 3".
        /// </summary>
        Each
    }

    LocationCardinality( int beg,
                         int end,
                         LocationKind kind,
                         int offset,
                         int expectedMatchCount )
        : base( beg, end )
    {
        Throw.DebugAssert( kind is not LocationKind.Single || expectedMatchCount == 1 );
        _kind = kind;
        _expectedMatchCount = expectedMatchCount;
        _offset = offset;
    }

    /// <summary>
    /// Gets this location kind.
    /// </summary>
    public LocationKind Kind => _kind;

    /// <summary>
    /// Gets the match number to consider among the multiple matches.
    /// Applies to "first [+]Offset" and "last [-]Offset" where it defaults to 1.
    /// For other <see cref="LocationKind"/>, this defaults to 0.
    /// </summary>
    public int Offset => _offset;

    /// <summary>
    /// Gets the total number of matches expected.
    /// Unapplicable when 0 (the default).
    /// Always 1 when <see cref="LocationKind.Single"/>.
    /// </summary>
    public int ExpectedMatchCount => _expectedMatchCount;

    internal static LocationCardinality? Match( ref TokenizerHead head, bool monoLocationOnly = false )
    {
        int begSpan = head.LastTokenIndex + 1;
        Token? kindT;
        Token? offsetT = null;
        Token? expectedMatchCountT = null;

        LocationKind kind;
        int offset = 0;
        int expectedMatchCount = 0;
        if( head.TryAcceptToken( "single", out kindT ) )
        {
            expectedMatchCount = 1;
            kind = LocationKind.Single;
        }
        else if( head.TryAcceptToken( "first", out kindT ) )
        {
            kind = LocationKind.First;
            head.TryAcceptToken( TokenType.Plus, out kindT );
            MatchFirstOrLastOptions( ref head, out offsetT, out offset, out expectedMatchCountT, out expectedMatchCount );
        }
        else if( head.TryAcceptToken( "last", out kindT ) )
        {
            kind = LocationKind.Last;
            head.TryAcceptToken( TokenType.Minus, out kindT );
            MatchFirstOrLastOptions( ref head, out offsetT, out offset, out expectedMatchCountT, out expectedMatchCount );
        }
        else if( head.TryAcceptToken( "all", out kindT ) )
        {
            kind = LocationKind.All;
            expectedMatchCount = TryMatchNumber( ref head, out expectedMatchCountT, 0 );
        }
        else if( head.TryAcceptToken( "each", out kindT ) )
        {
            kind = LocationKind.Each;
            expectedMatchCount = TryMatchNumber( ref head, out expectedMatchCountT, 0 );
        }
        else
        {
            return null;
        }
        if( monoLocationOnly && kind is LocationKind.All or LocationKind.Each )
        {
            head.AppendError( "Expected one-location specifier (single, first or last).", -1 );
            return null;
        }
        return head.AddSpan( new LocationCardinality( begSpan,
                                                      head.LastTokenIndex + 1,
                                                      kind,
                                                      offset,
                                                      expectedMatchCount ) );

        static int TryMatchNumber( ref TokenizerHead head, out Token? numberT, int n )
        {
            if( head.TryAcceptToken( TokenType.GenericNumber, out numberT ) )
            {
                if( !int.TryParse( numberT.Text.Span, out n ) )
                {
                    head.AppendError( "Too big number.", -1 );
                }
            }
            return n;
        }

        static void MatchFirstOrLastOptions( ref TokenizerHead head,
                                             out Token? offsetT,
                                             out int offset,
                                             out Token? expectedMatchCountT,
                                             out int expectedMatchCount )
        {
            offset = TryMatchNumber( ref head, out offsetT, 1 );
            expectedMatchCountT = null;
            expectedMatchCount = 0;
            if( head.TryAcceptToken( "out", out _ ) )
            {
                head.MatchToken( "of" );
                expectedMatchCount = TryMatchNumber( ref head, out expectedMatchCountT, 0 );
            }
        }
    }

}
