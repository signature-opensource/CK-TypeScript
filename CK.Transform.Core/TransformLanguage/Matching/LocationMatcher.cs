using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Captures "<see cref="LocationCardinality"/> <see cref="SpanMatcher"/>".
/// </summary>
public sealed partial class LocationMatcher : SourceSpan
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
    /// When null, the match is "single" (the <see cref="LocationCardinality.SingleCardinality"/> operator is used).
    /// </summary>
    public LocationCardinality? Cardinality => FirstChild as LocationCardinality;

    /// <summary>
    /// Gets the span matcher.
    /// Never null when <see cref="CheckValid()"/> is true.
    /// </summary>
    public SpanMatcher? Matcher
    {
        get
        {
            var c = FirstChild;
            return c as SpanMatcher ?? c?.NextSibling as SpanMatcher;
        }
    }

    internal static LocationMatcher? Parse( TransformLanguageAnalyzer analyzer, ref TokenizerHead head, bool monoLocationOnly = false )
    {
        int begSpan = head.LastTokenIndex + 1;
        var cardinality = LocationCardinality.Match( ref head, monoLocationOnly );
        var matcher = SpanMatcher.Match( analyzer, ref head );
        return matcher == null
                ? null
                : head.AddSpan( new LocationMatcher( begSpan, head.LastTokenIndex + 1 ) );
    }

}
