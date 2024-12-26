using System.Collections.Immutable;
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
    public static StringBuilder Write( this ImmutableArray<Token> tokens, StringBuilder b )
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
    /// Writes the text with one space trivia between tokens.
    /// </summary>
    /// <param name="tokens">These tokens.</param>
    /// <param name="b">The target builder.</param>
    /// <returns>The builder.</returns>
    public static StringBuilder WriteCompact( this ImmutableArray<Token> tokens, StringBuilder b )
    {
        bool hasWhitespace = true; 
        foreach( var t in tokens )
        {
            if( !hasWhitespace )
            {
                if( t.LeadingTrivias.Length > 0 )
                {
                    b.Append( ' ' );
                }
            }
            b.Append( t.Text );
            if( hasWhitespace = t.LeadingTrivias.Length > 0 )
            {
                b.Append( ' ' );
            }
        }
        return b;
    }
}
