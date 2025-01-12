using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.Core;

/// <summary>
/// Extends <see cref="List{T}"/> of tokens with useful helpers.
/// </summary>
public sealed class TokenListBuilder : List<Token>
{
    /// <summary>
    /// Clears the token <see cref="Token.TrailingTrivias"/> if it is a single ' ', by default from the last token.
    /// </summary>
    /// <param name="tokenIndex">Index of the token.</param>
    /// <returns>True if the trailing whitespace has been removed, false otherwise.</returns>
    public bool RemoveSingleTrailingWhitespace( int tokenIndex = -1 )
    {
        Throw.DebugAssert( Count > 0 );
        if( tokenIndex < 0 ) tokenIndex = Count - 1;
        var t = this[tokenIndex];
        if( t.TrailingTrivias.Length == 1 )
        {
            var trivia = t.TrailingTrivias[^1];
            if( trivia.IsWhitespace && trivia.Content.Length == 1 )
            {
                this[tokenIndex] = t.SetTrivias( trailing: Trivia.Empty );
                return true;
            }
        }
        return false;
    }
}

