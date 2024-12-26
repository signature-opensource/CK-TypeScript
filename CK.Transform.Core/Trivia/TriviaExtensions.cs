using System.Collections.Immutable;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Implements helpers for trivias.
/// </summary>
public static class TriviaExtensions
{
    /// <summary>
    /// Writes the trivias.
    /// </summary>
    /// <param name="trivias">These trivias.</param>
    /// <param name="b">The target builder.</param>
    /// <param name="oneSpaceTrivia">True to only write a space instead of the (non empty) trivias.</param>
    /// <returns>The builder.</returns>
    public static StringBuilder Write( this ImmutableArray<Trivia> trivias, StringBuilder b, bool oneSpaceTrivia = false )
    {
        if( oneSpaceTrivia )
        {
            if( trivias.Length > 0 ) b.Append( ' ' );
        }
        else
        {
            foreach( var t in trivias ) b.Append( t.Content );
        }
        return b;
    }
}
