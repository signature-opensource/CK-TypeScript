using CK.Transform.Core;

namespace CK.Html.Transform;

/// <summary>
/// Covers an attribute name and optional value. Xjhen parsed, this has a length of 1 (no value)
/// or 3 (name = value).
/// </summary>
class HtmlAttributeSpan : SourceSpan
{
    public HtmlAttributeSpan( int beg, int end )
        : base( beg, end )
    {
    }
}
