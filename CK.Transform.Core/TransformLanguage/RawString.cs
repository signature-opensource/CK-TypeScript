using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform;

/// <summary>
/// A raw string follows the same rules as the C# raw string: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/raw-string
/// except that 1 or 2 quotes are enough: you only need 3 quotes if the string contains "".
/// </summary>
public sealed class RawString : TokenNode, ITransformLanguageNode
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

}
