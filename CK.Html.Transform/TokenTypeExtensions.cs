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
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a Html starting element.</returns>
    public static bool IsHtmlStartingElement( this TokenType type ) => type == (TokenType)HtmlTokenType.StartingElement;

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
    public static bool IsHtmlClosingElement( this TokenType type ) => type == (TokenType)HtmlTokenType.ClosingElement;

    /// <summary>
    /// Gets whether this token is <c>&lt;tag/&gt;</c>.
    /// <para>
    /// It may contain whitespaces and an extra /: <c>&lt;/tag  /&gt;</c> is valid.
    /// </para>
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a Html auto closing element without attributes.</returns>
    public static bool IsHtmlEmptyAutoClosingElement( this TokenType type ) => type == (TokenType)(HtmlTokenType.StartingElement | HtmlTokenType.ClosingElement);

    /// <summary>
    /// Gets whether this token is <c>/&gt;</c> or <c>&gt;</c>.
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a closing tag.</returns>
    public static bool IsHtmlClosingTag( this TokenType type ) => type == (TokenType)HtmlTokenType.ClosingTag;
}
