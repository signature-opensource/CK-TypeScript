using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform;

public enum TokenType
{
    TransformClassNumber = Core.TokenType.MaxClassNumber - 1,
    TransformClassBit = 1 << TransformClassNumber,
    TransformClassMask = -1 << (31 - TransformClassNumber),

    Inject = TransformClassBit | 1,
    Into = TransformClassBit | 2,

    /// <summary>
    /// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/raw-string
    /// </summary>
    RawString = TransformClassBit | 3,
}

/// <summary>
/// A raw string follows the same rules as the C# raw string: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/raw-string
/// except that 1 or 2 quotes are enough: you only need 3 quotes if the string contains "".
/// </summary>
public sealed class RawString : TokenNode
{
    readonly ReadOnlyMemory<char> _innerText;
    readonly int _lineCount;

    internal RawString( ReadOnlyMemory<char> text,
                        ReadOnlyMemory<char> innerText,
                        int lineCount,
                        ImmutableArray<Trivia> leading,
                        ImmutableArray<Trivia> trailing )
        : base( (Core.TokenType)Transform.TokenType.RawString, text, leading, trailing )
    {
        _innerText = innerText;
        _lineCount = lineCount;
    }

    /// <summary>
    /// Gets the actual text string without the enclosing quotes.
    /// </summary>
    public ReadOnlyMemory<char> InnerText => _innerText;

    /// <summary>
    /// Gets the number of lines.
    /// </summary>
    public int LineCount => _lineCount;

    protected override AbstractNode DoClone( ImmutableArray<Trivia> leading, IList<AbstractNode>? content, ImmutableArray<Trivia> trailing )
    {
        return new RawString( Text, _innerText, _lineCount, leading, trailing );
    }

}
