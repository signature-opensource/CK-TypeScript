using CK.Transform.Core;

namespace CK.Html.Transform;

/// <summary>
/// Defines token class n°22 for html tokens.
/// </summary>
public enum HtmlTokenType
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
    /// Text token contains any text frgaments. There is no <see cref="TokenType.Whitespace"/> trivias
    /// in parsed Html.
    /// </summary>
    Text = HtmlClassBit,

    /// <summary>
    /// The token is a starting <c>&lt;element...</c>.
    /// If <see cref="ClosingElement"/> is not also set, then the following token is a <see cref="AttributeName"/>.
    /// </summary>
    StartingElement = HtmlClassBit | 1 << 7,

    /// <summary>
    /// The token is a closing <c>&lt;/element&gt;</c> or an auto-closing element
    /// <c>&lt;element/&gt;</c> without attributes after it if <see cref="StartingElement"/> is
    /// also set.
    /// <para>
    /// Applying https://html.spec.whatwg.org/multipage/parsing.html#parse-errors:
    /// <list type="bullet">
    ///     <item><term>end-tag-with-attributes</term><description>A faulty <c>&lt;/tag attr=4 &gt;</c> is handled.</description></item>
    ///     <item><term>end-tag-with-trailing-solidus</term><description>A faulty <c>&lt;/tag/&gt;</c> is handled.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    ClosingElement = HtmlClassBit | 1 << 6,

    /// <summary>
    /// The token is an attribute name. A very permissive syntax is implemented: an attribute name
    /// contains any characters except whitespace, equal, slash, &lt;, &amp;, &gt; (that end it).
    /// <para>
    /// This token is followed by a <see cref="TokenType.Equals"/> token when it has a value.
    /// </para>
    /// </summary>
    AttributeName = HtmlClassBit | 1 << 5,

    /// <summary>
    /// Attribute value can be unquoted or quoted with " or '.
    /// </summary>
    AttributeValue = HtmlClassBit | 1 << 4,
}
