using CK.Transform.Core;
using System;

namespace CK.Html.Transform;

/// <summary>
/// Extends <see cref="Token"/>.
/// </summary>
public static class TokenExtensions
{
    /// <summary>
    /// Gets the tag name if this token is a html starting or ending tag.
    /// </summary>
    /// <param name="t">This token.</param>
    /// <returns>The tag name.</returns>
    public static ReadOnlySpan<char> GetHtmlTagName( this Token t )
    {
        return (HtmlTokenType)t.TokenType switch
        {
            HtmlTokenType.StartingTag
            or HtmlTokenType.StartingVoidElement => t.Text.Span.Slice( 1 ),
            HtmlTokenType.StartingEmptyElement
            or HtmlTokenType.EmptyElement
            or HtmlTokenType.EmptyVoidElement => GetName( 1, t ),
            HtmlTokenType.EndingTag => GetName( 2, t ),
            _ => default
        };
        
        static ReadOnlySpan<char> GetName( int offset, Token t )
        {
            var s = t.Text.Span.Slice( offset );
            return s.Slice( 0, s.IndexOfAny( ' ', '/', '>' ) );
        }
    }

}
