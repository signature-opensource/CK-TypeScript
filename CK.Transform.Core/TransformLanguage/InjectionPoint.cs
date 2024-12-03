using CK.Transform.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.TransformLanguage;

public sealed class InjectionPoint : TokenNode
{
    internal InjectionPoint( ReadOnlyMemory<char> text, ImmutableArray<Trivia> leading = default, ImmutableArray<Trivia> trailing = default )
        : base( NodeType.GenericIdentifier, text, leading, trailing )
    {
    }

    public ReadOnlySpan<char> Name => Text.Span.Slice( 1, Text.Length - 2 );
}
