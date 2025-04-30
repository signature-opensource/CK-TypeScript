using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Implements helpers for <see cref="Token"/>.
/// </summary>
public static class TokenExtensions
{
    /// <summary>
    /// Writes the full text.
    /// </summary>
    /// <param name="tokens">These tokens.</param>
    /// <param name="b">The target builder.</param>
    /// <returns>The builder.</returns>
    public static StringBuilder Write( this ImmutableList<Token> tokens, StringBuilder b )
    {
        foreach( var t in tokens )
        {
            t.LeadingTrivias.Write( b );
            b.Append( t.Text );
            t.TrailingTrivias.Write( b );
        }
        return b;
    }

    /// <summary>
    /// Writes the full text.
    /// </summary>
    /// <param name="tokens">These tokens.</param>
    /// <param name="b">The target builder.</param>
    /// <returns>The builder.</returns>
    public static StringBuilder Write( this IEnumerable<Token> tokens, StringBuilder b )
    {
        foreach( var t in tokens )
        {
            t.LeadingTrivias.Write( b );
            b.Append( t.Text );
            t.TrailingTrivias.Write( b );
        }
        return b;
    }

    /// <summary>
    /// Gets the full text of these tokens (trivias and token text).
    /// </summary>
    /// <param name="tokens">These tokens.</param>
    /// <returns>The full text.</returns>
    public static string ToFullString( this IEnumerable<Token> tokens ) => Write( tokens, new StringBuilder() ).ToString();

    /// <summary>
    /// Writes the text with a single character that replaces trivias between tokens.
    /// </summary>
    /// <param name="tokens">These tokens.</param>
    /// <param name="b">The target builder.</param>
    /// <param name="trivia">The character that replaces trivias.</param>
    /// <returns>The builder.</returns>
    public static StringBuilder WriteCompact( this IEnumerable<Token> tokens, StringBuilder b, char trivia = ' ' )
    {
        bool hasWhitespace = true; 
        foreach( var t in tokens )
        {
            if( !hasWhitespace )
            {
                if( t.LeadingTrivias.Length > 0 )
                {
                    b.Append( trivia );
                }
            }
            b.Append( t.Text );
            if( hasWhitespace = t.LeadingTrivias.Length > 0 )
            {
                b.Append( trivia );
            }
        }
        return b;
    }

    /// <summary>
    /// Gets the text of these tokens without trivias: trivias are replaced by <paramref name="trivia"/>.
    /// </summary>
    /// <param name="tokens">These tokens.</param>
    /// <param name="trivia">The character that replaces trivias.</param>
    /// <returns>The compact text.</returns>
    public static string ToCompactString( this IEnumerable<Token> tokens, char trivia = ' ' ) => WriteCompact( tokens, new StringBuilder(), trivia ).ToString();


    /// <summary>
    /// Creates a clone of this token with the new trivias. When let to the <c>default</c>, the current trivias are preserved.
    /// </summary>
    /// <typeparam name="T">This token type.</typeparam>
    /// <param name="token">This token.</param>
    /// <param name="leading">New leading trivia.</param>
    /// <param name="trailing">New trailing trivia.</param>
    /// <returns>A new immutable Token or this if no change occurred.</returns>
    static public T SetTrivias<T>( this T token, ImmutableArray<Trivia> leading = default, ImmutableArray<Trivia> trailing = default ) where T : Token
    {
        if( leading.IsDefault )
        {
            if( trailing.IsDefault ) return token;
            leading = token.LeadingTrivias;
        }
        else if( trailing.IsDefault ) trailing = token.TrailingTrivias;
        return Unsafe.As<T>( token.CloneForTrivias( leading, trailing ) );
    }
}
