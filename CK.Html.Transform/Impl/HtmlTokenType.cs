using CK.Transform.Core;

namespace CK.Html.Transform;

/// <summary>
/// Defines token class n°22 for html tokens.
/// This is internal. <see cref="TokenTypeExtensions"/> exposes them as extension methods.
/// </summary>
enum HtmlTokenType
{
    None = 0,

    /// <summary>
    /// Html class token is n°22. 8 bits (256 values) are avalaible.
    /// </summary>
    HtmlClassNumber = 22,

    /// <summary>
    /// Html class bit.
    /// </summary>
    HtmlClassBit = 1 << (31 - HtmlClassNumber),

    /// <summary>
    /// Html class mask.
    /// </summary>
    HtmlClassMask = -1 << (31 - HtmlClassNumber),

    /// <summary>
    /// Text token contains any text fragments. There is no <see cref="TokenType.Whitespace"/> trivias
    /// in parsed Html.
    /// </summary>
    Text = HtmlClassBit,

    /// <summary>
    /// The token is a starting <c>&lt;tag</c> element with at least one attribute.
    /// <para>
    /// Per https://html.spec.whatwg.org/multipage/syntax.html#syntax-tag-name:
    /// <list type="bullet">
    ///     <item>
    ///     Tags contain a tag name, giving the element's name. HTML elements all have names that only use ASCII alphanumerics. In the HTML syntax, tag names,
    ///     even those for foreign elements, may be written with any mix of lower- and uppercase letters that, when converted to all-lowercase, matches the element's
    ///     tag name; tag names are case-insensitive.
    ///     </item>
    /// </list>
    /// Unfortuntately, this is not that simple. Custom-elements are (https://html.spec.whatwg.org/dev/custom-elements.html#custom-elements-core-concepts)
    /// can contain - (and they do!) and a lot of other characters:
    /// <code>
    /// PotentialCustomElementName ::=
    ///     [a-z] (PCENChar)* '-' (PCENChar)*
    ///     PCENChar ::= "-" | "." | [0 - 9] | "_" | [a-z] | #xB7 | [#xC0-#xD6]
    ///                | [#xD8-#xF6] | [#xF8-#x37D] | [#x37F-#x1FFF] | [#x200C-#x200D]
    ///                | [#x203F-#x2040] | [#x2070-#x218F] | [#x2C00-#x2FEF] | [#x3001-#xD7FF]
    ///                | [#xF900-#xFDCF] | [#xFDF0-#xFFFD] | [#x10000-#xEFFFF]
    /// </code>
    /// Moreover, browsers are very permissive: &lt;a-🙃&gt;Normally invalid... but works in most browsers.&lt;/a-🙃&gt; but it seems that the first character
    /// MUST definitely be in [a-zA-Z].
    /// </para>
    /// <para>
    /// All this considered, we expect tag name to start with [a-zA-Z] (or ignore the opening '&lt;' and considering as <see cref="Text"/>) but we
    /// allow any characters to follow, up to a whitespace, /, &lt; or &gt;.
    /// </para>
    /// </summary>
    StartingTag = HtmlClassBit | 1 << 7,

    /// <summary>
    /// The token is a closing <c>&lt;/element&gt;</c>.
    /// <para>
    /// Applying https://html.spec.whatwg.org/multipage/parsing.html#parse-errors:
    /// <list type="bullet">
    ///     <item><term>end-tag-with-attributes</term><description>A faulty <c>&lt;/tag attr=4 &gt;</c> is handled.</description></item>
    ///     <item><term>end-tag-with-trailing-solidus</term><description>A faulty <c>&lt;/tag/&gt;</c> is handled.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    EndingTag = HtmlClassBit | 1 << 6,

    /// <summary>
    /// The token is a <c>&lt;element/&gt;</c> without attributes.
    /// <para>
    /// In terms of structure, it is the same as a <see cref="EmptyVoidElement"/> but whether the tag is actually one of the void elements
    /// is not tested.
    /// </para>
    /// </summary>
    EmptyElement = StartingTag | EndingTag,

    /// <summary>
    /// The token is <c>&lt;element&gt;</c> without attributes.
    /// </summary>
    StartingEmptyElement = HtmlClassBit | 1 << 5,

    /// <summary>
    /// The token is <c>/&gt;</c>. 
    /// </summary>
    EndTokenTag = HtmlClassBit | 1 << 4,

    /// <summary>
    /// Void elements are <c>&lt;tag&gt;</c> without attributes where tag is:
    /// <c>area</c>, <c>base</c>, <c>br</c>, <c>col</c>, <c>embed</c>, <c>hr</c>, <c>img</c>,
    /// <c>input</c>, <c>link</c>, <c>meta</c>, <c>source</c>, <c>track</c>, or <c>wbr</c>.
    /// </summary>
    EmptyVoidElement = HtmlClassBit | 1 << 3,

    /// <summary>
    /// The token starts a void element: <c>&lt;br</c> with at least one attribute.
    /// </summary>
    StartingVoidElement = StartingTag | EmptyVoidElement
}
