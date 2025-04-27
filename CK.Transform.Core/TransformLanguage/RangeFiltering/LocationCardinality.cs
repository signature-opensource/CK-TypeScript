using CK.Core;
using System;

namespace CK.Transform.Core;

/// <summary>
/// Models location cardinality constraint:
/// <list type="bullet">
///     <item>single</item>
///     <item>first [+n] [out of n]</item>
///     <item>last [-n] [out of n]</item>
///     <item>all [n]</item>
///     <item>each [n]</item>
/// </list>
/// </summary>
public sealed class LocationCardinality : SourceSpan
{
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
        /// First location or the nth location ("first +2" is the second location).
        /// <see cref="ExpectedMatchCount"/> applies when not 0: "first +2 out of 3".
        /// </summary>
        First,

        /// <summary>
        /// First location or the nth location from the last one ("last -2" is the penultimate).
        /// <see cref="ExpectedMatchCount"/> applies when not 0: "last -2 out of 3".
        /// </summary>
        Last,

        /// <summary>
        /// All possible locations: the different ranges are considered as a unique
        /// range (with holes in it).
        /// <see cref="ExpectedMatchCount"/> applies when not 0: "all 3".
        /// </summary>
        All,

        /// <summary>
        /// Each possible locations: the different ranges are independent
        /// from each other.
        /// <see cref="ExpectedMatchCount"/> applies when not 0: "each 3".
        /// </summary>
        Each
    }

    int _expectedMatchCount;
    int _offset;
    LocationKind _kind;

    internal LocationCardinality( int beg,
                                  int end,
                                  LocationKind kind,
                                  int offset,
                                  int expectedMatchCount )
        : base( beg, end )
    {
        _kind = kind;
        _expectedMatchCount = expectedMatchCount;
        _offset = offset;
    }

    /// <summary>
    /// Gets or sets this location kind.
    /// </summary>
    public LocationKind Kind
    {
        get => _kind;
        set
        {
            if( value is LocationKind.First or LocationKind.Last )
            {
                if( _offset == 0 ) _offset = 1;
            }
            else
            {
                _offset = 0;
            }
            _kind = value;
        }
    }

    /// <summary>
    /// Gets or sets the total number of matches expected.
    /// Unapplicable when 0 (the default).
    /// Always 1 when <see cref="LocationKind.Single"/>.
    /// </summary>
    public int ExpectedMatchCount
    {
        get => _kind is LocationKind.Single ? 1 : _expectedMatchCount;
        set
        {
            Throw.CheckArgument( value >= 0 );
            _expectedMatchCount = value;
        }
    }

    /// <summary>
    /// Gets or sets the match number to consider among the multiple matches.
    /// Applies to "first [+]Offset" and "last [-]Offset" where it defaults to 1.
    /// For other <see cref="LocationKind"/>, this defaults to 0.
    /// </summary>
    public int Offset
    {
        get => _offset;
        set
        {
            Throw.CheckArgument( value >= 0 );
            if( value == 0 && _kind is LocationKind.First or LocationKind.Last )
            {
                value = 1;
            }
            _offset = value;
        }
    }

    internal static LocationCardinality? TryMatch( ref TokenizerHead head, bool monoLocationOnly = false )
    {
        int begSpan = head.LastTokenIndex + 1;
        LocationKind kind;
        int offset = 0;
        int expectedMatchCount = 0;
        if( head.TryAcceptToken( "single", out _ ) )
        {
            kind = LocationKind.Single;
        }
        else if( head.TryAcceptToken( "first", out _ ) )
        {
            kind = LocationKind.First;
            head.TryAcceptToken( TokenType.Plus, out _ );
            MatchFirstOrLastOptions( ref head, out offset, out expectedMatchCount );
        }
        else if( head.TryAcceptToken( "last", out _ ) )
        {
            kind = LocationKind.Last;
            head.TryAcceptToken( TokenType.Minus, out _ );
            MatchFirstOrLastOptions( ref head, out offset, out expectedMatchCount );
        }
        else if( head.TryAcceptToken( "all", out _ ) )
        {
            kind = LocationKind.All;
            expectedMatchCount = TryMatchNumber( ref head, 0 );
        }
        else if( head.TryAcceptToken( "each", out _ ) )
        {
            kind = LocationKind.Each;
            expectedMatchCount = TryMatchNumber( ref head, 0 );
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
        return new LocationCardinality( begSpan, head.LastTokenIndex + 1, kind, offset, expectedMatchCount );

        static int TryMatchNumber( ref TokenizerHead head, int n )
        {
            if( head.TryAcceptToken( TokenType.GenericNumber, out var sNum ) )
            {
                if( !int.TryParse( sNum.Text.Span, out n ) )
                {
                    head.AppendError( "Too big number.", -1 );
                }
            }
            return n;
        }

        static void MatchFirstOrLastOptions( ref TokenizerHead head, out int offset, out int expectedMatchCount )
        {
            offset = TryMatchNumber( ref head, 1 );
            expectedMatchCount = 0;
            if( head.TryAcceptToken( "out", out _ ) )
            {
                head.MatchToken( "of" );
                expectedMatchCount = TryMatchNumber( ref head, 0 );
            }
        }
    }


    /// <summary>
    /// Gets the formatted string (as it can be parsed).
    /// </summary>
    /// <returns>The readable (and parsable) string.</returns>
    public override string ToString()
    {
        return _kind switch
        {
            LocationKind.Each => _expectedMatchCount == 0 ? "each" : "each " + _expectedMatchCount,
            LocationKind.All => _expectedMatchCount == 0 ? "all" : "all " + _expectedMatchCount,
            LocationKind.Single => "single",
            _ => FirstOrLast( _kind is LocationKind.First, _offset, _expectedMatchCount )
        };

        static string FirstOrLast( bool first, int offset, int expectedMatchCount )
        {
            string s = first
                        ? (offset == 0 ? "first" : "first +" + offset)
                        : (offset == 0 ? "last" : "last -" + offset);
            if( expectedMatchCount > 0 ) s += " out of " + expectedMatchCount;
            return s;
        }
    }

}
