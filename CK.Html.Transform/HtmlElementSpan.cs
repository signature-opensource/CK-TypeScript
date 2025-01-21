using CK.Transform.Core;

namespace CK.Html.Transform;

/// <summary>
/// Covers an Html element with its attributes, children and closing tag if any.
/// </summary>
public class HtmlElementSpan : SourceSpan
{
    public HtmlElementSpan( int beg, int end )
        : base( beg, end )
    {
    }
}
