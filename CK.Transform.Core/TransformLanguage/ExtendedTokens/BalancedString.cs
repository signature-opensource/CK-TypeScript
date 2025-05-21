using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Very basic token that can handle {some { string }} strings.
/// </summary>
public sealed class BalancedString : Token
{
    BalancedString( ReadOnlyMemory<char> text,
                    ImmutableArray<Trivia> leading,
                    ImmutableArray<Trivia> trailing )
    : base( TokenType.GenericString, leading, text, trailing )
    {
    }

    /// <summary>
    /// Gets the opening quote character.
    /// </summary>
    public char OpeningQuote => Text.Span[0];

    /// <summary>
    /// Gets the closing quote character.
    /// </summary>
    public char ClosingQuote => Text.Span[^1];

    /// <summary>
    /// Gets the string content as a memory.
    /// </summary>
    public ReadOnlyMemory<char> InnerMemoryText => Text[1..^1];

    /// <summary>
    /// Gets the string content as a span.
    /// </summary>
    public ReadOnlySpan<char> InnerText => Text.Span[1..^1];

    /// <summary>
    /// Tries to match a <see cref="BalancedString"/> token.
    /// </summary>
    /// <param name="head">The head.</param>
    /// <param name="openingQuote">The opening character.</param>
    /// <param name="closingQuote">The closing character.</param>
    /// <returns>
    /// The string or null if head is not starting with <paramref name="openingQuote"/> or an <see cref="TokenType.ErrorUnterminatedString"/>
    /// error has been added.
    /// </returns>
    public static BalancedString? TryMatch( ref TokenizerHead head, char openingQuote, char closingQuote )
    {
        Throw.CheckArgument( openingQuote != closingQuote );
        var h = head.Head;
        if( h.Length == 0 || h[0] != openingQuote )
        {
            return null;
        }
        h = h.Slice( 1 );
        int depth = 1;
        for(; ; )
        {
            int iE = h.IndexOf( closingQuote );
            if( iE < 0 )
            {
                head.AppendError( "Unterminated string.", 1, TokenType.ErrorUnterminatedString );
                return null;
            }
            depth += h.Slice( 0, iE ).Count( openingQuote ) - 1;
            h = h.Slice( iE + 1 );
            if( depth == 0 ) break;
        }
        int pos;
        if( h.Length == 0 ) pos = head.Head.Length;
        else head.Head.Overlaps( h, out pos );
        head.PreAcceptToken( pos, out var text, out var leading, out var trailing );
        return head.Accept( new BalancedString( text, leading, trailing ) );
    }

}
