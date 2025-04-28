using CK.Core;
using System.Diagnostics.CodeAnalysis;

namespace CK.Transform.Core;

/// <summary>
/// Captures "<see cref="LocationCardinality"/> <see cref="Matcher"/>".
/// </summary>
public sealed class LocationMatcher : SourceSpan
{
    LocationMatcher( int beg, int end )
        : base( beg, end )
    {
    }

    [MemberNotNull( nameof( Matcher ) )]
    public override void CheckValid()
    {
        base.CheckValid();
        Throw.CheckState( Matcher != null );
    }

    /// <summary>
    /// Gets the optional match cardinality.
    /// When null, defaults to <see cref="LocationCardinality.LocationKind.Single"/>.
    /// </summary>
    public LocationCardinality? Cardinality => Children.FirstChild as LocationCardinality;

    /// <summary>
    /// Gets the span matcher.
    /// Never null when <see cref="CheckValid()"/> doesn't throw.
    /// </summary>
    public SpanMatcher? Matcher
    {
        get
        {
            var c = Children.FirstChild;
            return c as SpanMatcher ?? c?.NextSibling as SpanMatcher;
        }
    }

    internal static LocationMatcher? Match( ref TokenizerHead head, bool monoLocationOnly = false )
    {
        int begSpan = head.LastTokenIndex + 1;
        var cardinality = LocationCardinality.Match( ref head, monoLocationOnly );
        var matcher = SpanMatcher.Match( ref head );
        return matcher == null
                ? null
                : head.AddSpan( new LocationMatcher( begSpan, head.LastTokenIndex + 1 ) );
    }
}
