using System;
using System.Collections.Immutable;

namespace CK.Transform.Core;


/// <summary>
/// An injection point <see cref="Name"/> is defined between angle brackets: &lt;Name&gt;.
/// </summary>
public sealed class InjectionPoint : Token
{
    internal InjectionPoint( ReadOnlyMemory<char> text, ImmutableArray<Trivia> leading = default, ImmutableArray<Trivia> trailing = default )
        : base( TokenType.GenericIdentifier, leading, text, trailing )
    {
    }

    /// <summary>
    /// Get the injection point name without enclosing angle brackets 
    /// (the <see cref="Token.Text"/> has the brackets).
    /// </summary>
    public ReadOnlySpan<char> Name => Text.Span.Slice( 1, Text.Length - 2 );
}
