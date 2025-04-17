using CK.Transform.Core;

namespace CK.Html.Transform;

/// <summary>
/// Covers an Html element with its attributes, children and closing tag if any.
/// </summary>
public class HtmlElementSpan : SourceSpan
{
    /// <summary>
    /// Initializes a new HtmlElementSpan.
    /// </summary>
    /// <param name="beg">The start of the span. Must be greater or equal to 0.</param>
    /// <param name="end">The end of the span. Must be greater than <paramref name="beg"/>.</param>
    public HtmlElementSpan( int beg, int end )
        : base( beg, end )
    {
    }
}
