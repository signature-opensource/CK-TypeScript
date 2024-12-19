using CK.Transform.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// An injection point <see cref="Name"/> is defined between angle brackets: &lt;Name&gt;.
/// </summary>
public sealed class InjectionPointOld : TokenNode
{
    internal InjectionPointOld( ReadOnlyMemory<char> text, ImmutableArray<Trivia> leading = default, ImmutableArray<Trivia> trailing = default )
        : base( TokenType.GenericIdentifier, text, leading, trailing )
    {
    }

    /// <summary>
    /// Get the injection point name without enclosing angle brackets 
    /// (the <see cref="TokenNode.Text"/> has the brackets).
    /// </summary>
    public ReadOnlySpan<char> Name => Text.Span.Slice( 1, Text.Length - 2 );
}
