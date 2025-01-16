using CK.Transform.Core;

namespace CK.Html.Transform;

/// <summary>
/// Covers an Html element.
/// When parsed, the last token is not necessarily a <see cref="HtmlTokenType.ClosingElement"/>
/// because of void elements <c>area</c>, <c>base</c>, <c>br</c>, <c>col</c>, <c>embed</c>, <c>hr</c>,
/// <c>img</c>, <c>input</c>, <c>link</c>, <c>meta</c>, <c>source</c>, track</c>, wbr</c>
/// (see https://html.spec.whatwg.org/multipage/syntax.html#elements-2) and because
/// of https://html.spec.whatwg.org/multipage/syntax.html#optional-tags.
/// </summary>
public class HtmlElementSpan : SourceSpan
{
    public HtmlElementSpan( int beg, int end )
        : base( beg, end )
    {
    }
}
