using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// A raw string follows the same rules as the C# raw string: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/raw-string.
/// <list type="bullet">
///     <item>A single quote opens a "single-line string": the closing quote must appear before the end-of-line.</item>
///     <item>Two consecutive quotes is the empty string "".</item>
///     <item>Three quotes or more opens a multi-line string that can contain consecutive quotes sequence shorter than the opening quotes.</item>
/// </list>
/// <para>
/// Escape character '\' is a regular character: thanks to the <c>"""raw "string""""</c> it is useless, <c>"""\""""</c> is the string '\"'.
/// </para>
/// <para>
/// Note that in C#, <c>"""raw "string""""</c> is not valid (but valid for us).
/// </para>
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
        : base( TokenType.GenericString, text, leading, trailing )
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
