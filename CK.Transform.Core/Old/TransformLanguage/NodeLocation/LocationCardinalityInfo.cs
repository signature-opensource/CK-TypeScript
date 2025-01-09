namespace CK.Transform.Core;

/// <summary>
/// Captures a normalized cardinality for multi (all, each) and single (first, last, single) matches.
/// </summary>
public readonly struct LocationCardinalityInfo
{
    /// <summary>
    /// Total number of matches expected.
    /// Unapplicable when 0.
    /// </summary>
    public readonly int ExpectedMatchCount;

    /// <summary>
    /// The match number to consider among the multiple matches.
    /// Actual match depends on <see cref="FromFirst"/>: "first +Offset" or "last -Offset" (when FromFirst is false).
    /// </summary>
    public readonly int Offset;

    /// <summary>
    /// States whether <see cref="Offset"/> is from the first match or must be found in reverse order.
    /// </summary>
    public readonly bool FromFirst;

    /// <summary>
    /// Gets whether all the matches are concerned. Always true if <see cref="Each"/> is true.
    /// </summary>
    public readonly bool All;

    /// <summary>
    /// Gets whether all the matches are concerned but in sequence: each match is an independent range
    /// instead of bein considered as a multi-parts range.
    /// </summary>
    public readonly bool Each;

    /// <summary>
    /// Gets whether this info has been initialized.
    /// </summary>
    public readonly bool IsValid;

    internal LocationCardinalityInfo( bool isSingle, bool isFirst, int offset, int expectedMatchCount )
    {
        FromFirst = All = Each = false;
        if( isSingle )
        {
            // "single" is the same as "first out of 1".
            ExpectedMatchCount = 1;
            FromFirst = true;
        }
        else
        {
            ExpectedMatchCount = expectedMatchCount;
            if( isFirst )
            {
                FromFirst = true;
            }
            // else it is SqlTokenType.Last: FromFirst = All = Each = false;
        }
        Offset = offset;
        IsValid = true;
    }

    internal LocationCardinalityInfo( bool isEach, int expectedMatchCount )
    {
        FromFirst = All = true;
        ExpectedMatchCount = expectedMatchCount;
        Each = isEach;
        IsValid = true;
    }

    /// <summary>
    /// Initializes a "single" cardinality.
    /// </summary>
    public LocationCardinalityInfo()
    {
        ExpectedMatchCount = 1;
        FromFirst = true;
        IsValid = true;
    }

    /// <summary>
    /// Gets the formatted string (as it can be parsed).
    /// </summary>
    /// <returns>The readable (and parsable) string.</returns>
    public override string ToString()
    {
        if( Each )
        {
            return ExpectedMatchCount == 0 ? "each" : "each " + ExpectedMatchCount;
        }
        if( All )
        {
            return ExpectedMatchCount == 0 ? "all" : "all " + ExpectedMatchCount;
        }
        string s;
        if( FromFirst )
        {
            if( Offset == 0 && ExpectedMatchCount == 1 ) return "single";
            s = Offset == 0 ? "first" : "first+" + Offset;
            if( ExpectedMatchCount > 0 ) s += " out of " + ExpectedMatchCount;
            return s;
        }
        s = Offset == 0 ? "last" : "last-" + Offset;
        if( ExpectedMatchCount > 0 ) s += " out of " + ExpectedMatchCount;
        return s;
    }

}


