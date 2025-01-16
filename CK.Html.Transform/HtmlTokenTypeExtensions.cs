using CK.Transform.Core;

namespace CK.Html.Transform;

public static class HtmlTokenTypeExtensions
{
    static HtmlTokenTypeExtensions()
    {
        TokenTypeExtensions.ReserveTokenClass( 22, "Html" );
    }

    /// <summary>
    /// Gets whether this token type is a <see cref="HtmlTokenType"/>, possibly on
    /// error (<see cref="TokenType.ClassMaskAllowError"/> is used).
    /// </summary>
    /// <param name="type">This token type.</param>
    /// <returns>True if this token is a Html token.</returns>
    public static bool IsHtmlTokenType( this TokenType type ) => (type & TokenType.ClassMaskAllowError) == (TokenType)HtmlTokenType.HtmlClassBit;

}
