using CK.Transform.Core;
using System.ComponentModel;

namespace CK.Html.Transform;

public static class TokenTypeExtensions
{
    static TokenTypeExtensions()
    {
        CK.Transform.Core.TokenTypeExtensions.ReserveTokenClass( 22, "Html" );
    }

    /// <summary>
    /// Gets whether this token type is a <see cref="HtmlTokenType"/>, possibly on
    /// error (<see cref="TokenType.ClassMaskAllowError"/> is used).
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a Html token.</returns>
    public static bool IsHtmlTokenType( this TokenType type ) => (type & TokenType.ClassMaskAllowError) == (TokenType)HtmlTokenType.HtmlClassBit;

    /// <summary>
    /// Gets whether this token is a html text fragment. Its content in not decoded. It contains whitespaces (no trivias).
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a Html text fragment.</returns>
    public static bool IsHtmlText( this TokenType type ) => type == (TokenType)HtmlTokenType.Text;

    /// <summary>
    /// Gets whether this token is <c>&lt;tag</c>.
    /// The next token is a <see cref="TokenType.GenericIdentifier"/> that is the first attribute name that carries leading whitespaces trivias.
    /// After attributes, a token <see cref="TokenType.GreaterThan"/> or <see cref="IsHtmlEndTokenTag(TokenType)"/> closes the element span.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a Html starting tag.</returns>
    public static bool IsHtmlStartingTag( this TokenType type ) => type == (TokenType)HtmlTokenType.StartingTag;

    /// <summary>
    /// Gets whether this token is <c>&lt;/tag&gt;</c>.
    /// <para>
    /// It may contain whitespaces: <c>&lt;/tag  &gt;</c> and even
    /// attributes and an extra slash: applying https://html.spec.whatwg.org/multipage/parsing.html#parse-errors:
    /// <list type="bullet">
    ///     <item><term>end-tag-with-attributes</term><description>A faulty <c>&lt;/tag attr=4 &gt;</c> is handled.</description></item>
    ///     <item><term>end-tag-with-trailing-solidus</term><description>A faulty <c>&lt;/tag/&gt;</c> is handled.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a Html closing element.</returns>
    public static bool IsHtmlEndingTag( this TokenType type ) => type == (TokenType)HtmlTokenType.EndingTag;

    /// <summary>
    /// The token is <c>&lt;element/&gt;</c> without attributes.
    /// <para>
    /// In terms of structure, it is the same as a <see cref="IsEmptyVoidElement"/> but whether the tag is actually
    /// one of the void elements is not tested.
    /// </para>
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a Html empty element.</returns>
    public static bool IsHtmlEmptyElement( this TokenType type ) => type == (TokenType)HtmlTokenType.EmptyElement;

    /// <summary>
    /// The token is <c>&lt;element&gt;</c> without attributes.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a Html empty element.</returns>
    public static bool IsHtmlStartingEmptyElement( this TokenType type ) => type == (TokenType)HtmlTokenType.StartingEmptyElement;

    /// <summary>
    /// The token is one of <c>area</c>, <c>base</c>, <c>br</c>, <c>col</c>, <c>embed</c>, <c>hr</c>,
    /// <c>img</c>, <c>input</c>, <c>link</c>, <c>meta</c>, <c>source</c>, <c>track</c>, <c>wbr</c> element
    /// without attributes.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a Html void element without attribute.</returns>
    public static bool IsEmptyVoidElement( this TokenType type ) => type == (TokenType)HtmlTokenType.EmptyVoidElement;

    /// <summary>
    /// The token starts a void element: <c>&lt;br</c>. There is at least one attribute. The closing tag should be
    /// <c>&gt;</c> but may be <c>/&gt;</c>.
    /// </summary>
    public static bool IsStartingVoidElement( this TokenType type ) => type == (TokenType)HtmlTokenType.StartingVoidElement;

    /// <summary>
    /// The token is <c>/&gt;</c>.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a closing tag.</returns>
    public static bool IsHtmlEndTokenTag( this TokenType type ) => type == (TokenType)HtmlTokenType.EndTokenTag;
}
